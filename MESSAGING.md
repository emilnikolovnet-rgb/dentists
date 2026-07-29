# Messaging: outbox and the dentist-assignment saga

This service consumes the Appointments service's integration events and assigns a real dentist
to every appointment booked there.

## Why it is built this way

The Appointments events name no dentist. `AppointmentCreated` carries an appointment id, a
patient name and a time — nothing that identifies who will see the patient. But an appointment
here only exists embedded inside a dentist's document, so one has to be chosen before anything
can be recorded, and remembered afterwards: `AppointmentConfirmed` names no dentist either.
Holding that mapping is the saga's whole reason to exist. Without it, every confirmation would
mean scanning all dentists across all partitions to find who holds the booking.

MassTransit's transactional outbox could not be reused. The Appointments service uses
`AddEntityFrameworkOutbox(o => o.UseSqlServer())`; there is no Cosmos provider. Putting the
outbox in a SQL sidecar would leave the business write in Cosmos and the message write in SQL —
two transactions, which is the dual-write problem an outbox exists to remove. So the outbox is
embedded in the dentist document, sharing its partition key, and commits in the same Cosmos
write as the change it announces.

## Flow

```
AppointmentCreated  ──► saga: Reserving
                          └─ ReserveDentist ──► picks a free dentist,
                                                books the slot, queues DentistReserved
                                                  ── all one Cosmos write ──
                       OutboxDispatcher publishes it
                          │
     ┌────────────────────┴─────────────────────┐
DentistReserved                        DentistReservationFailed
  saga: AwaitingConfirmation             saga: Failed
  └─ ConfirmAppointmentRequested ─►      └─ CancelAppointment ─► Appointments
     Appointments                           (compensation)

AppointmentConfirmed ─► saga ─► SetDentistAppointmentStatus(Confirmed)
AppointmentCancelled ─► saga ─► SetDentistAppointmentStatus(Cancelled)
AppointmentUpdated   ─► saga ─► RescheduleDentistAppointment
```

The saga never touches the database. It only routes messages, so that every change to a
dentist happens inside a consumer where the business change, the inbox entry and the outbox
message commit together.

## Running it

**MassTransit is pinned to 8.5.10 on purpose.** Version 9 refuses to start the bus without a
commercial licence (`MT_LICENSE` / `MT_LICENSE_PATH`); 8.5.10 is the last Apache-2.0 release.
The message envelope is unchanged between 8 and 9, so this service still interoperates with
the Appointments service while that one remains on 9.1.2 — but note that service needs a
licence of its own before its bus will start.

Do not upgrade the MassTransit packages without deciding about the licence first.

Configuration:

| Setting | Purpose |
|---|---|
| `SERVICE_BUS_CONNECTION_STRING` | Azure Service Bus. Same key name the Appointments service uses. |
| `ConnectionStrings:DefaultConnection` | Cosmos. Also supplies the saga repository's endpoint and key, split by `CosmosConnectionString`. |
| `Cosmos:DatabaseName` | Holds both the dentists container and `dentist-assignment-saga`. |
| `DentistAssignment:AppointmentDuration` | How long a dentist is busy from an appointment's start. The Appointments service publishes only a start time, so the length is this service's assumption. |
| `Outbox:PollInterval` / `BatchSize` / `RetentionPeriod` | Dispatcher sweep timing, batch size, and how long dispatched and consumed entries are kept before pruning. |

The saga container is created on demand by MassTransit; the dentists container is not — see the
`EnsureCreatedAsync` call in `Program.cs`, which only runs in Development.

## Things worth knowing before changing it

**Pruning is not optional.** The outbox and inbox live inside a document with a hard 2 MB
ceiling. `Dentist.PruneMessages` runs on every dispatcher sweep. Removing it eventually makes
dentists unwritable.

**`RetentionPeriod` must exceed the transport's redelivery window**, or a late redelivery is no
longer recognised as a duplicate and gets applied twice.

**Delivery is at least once, deliberately.** The dispatcher publishes and then marks dispatched,
so a crash in between republishes rather than loses. That is why consumers keep an inbox.

**Contract namespaces are load-bearing.** The mirrored records in `Dentists.Contracts` sit under
`Appointments.Application.*` because MassTransit routes on namespace plus type name. Renaming
them to match this project silently stops the messages binding.

**The outbox query deliberately ignores the soft-delete filter.** A deleted dentist can still
hold messages its last changes committed to publishing.

**The saga uses `SendAsync`, never `Send`.** `context.Init<T>()` returns a `Task`, and the
synchronous `Send` overload happily binds against that `Task` as though it were the message.
It compiles, then fails at run time looking for an endpoint for
`Task<SendTuple<T>>` — so every transition out of the saga faults and nothing moves.

## Known gaps

- **Reassignment on reschedule.** A rescheduled booking stays with its dentist. If that dentist
  is then double-booked, the move is still applied — the Appointments service has already
  committed to the new time and refusing here would leave the two disagreeing — and the
  collision is logged as needing manual reassignment. Releasing and re-reserving elsewhere is
  the open piece.
- **The saga's own sends are not transactional.** Only writes going through a dentist document
  get outbox protection. Between saga state persistence and a send, MassTransit redelivery is
  the only recovery.
- **Reservation is idempotent by lookup, not by lock.** A redelivered `ReserveDentist` finds the
  existing holder, but two genuinely concurrent deliveries could pick different dentists. Cosmos
  offers optimistic concurrency only; the pessimistic row lock the Appointments saga uses has no
  equivalent here.
- **Compensation uses `CancelAppointment`**, an internal command of the Appointments saga rather
  than a contract it advertises. A public `CancelAppointmentRequested`, symmetrical with
  `ConfirmAppointmentRequested`, is the tidier shape and needs a change on that side.
