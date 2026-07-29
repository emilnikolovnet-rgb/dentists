# Dentists.Contracts

Message contracts exchanged over Azure Service Bus.

MassTransit resolves a message type to a transport address from its **namespace + type name**
(`urn:message:Namespace:TypeName`). Two services only exchange a message if both declare it
under the identical namespace, so the namespaces here are not a stylistic choice.

## Two kinds of contract live here

**`Appointments.Application.Events` / `Appointments.Application.Messages`** — owned by the
Appointments service, mirrored here so this service can consume them. The namespaces
deliberately do not match this project's name: they match the Appointments service, because
that is what makes routing work. Do not "tidy" them.

Keep these in step with
`AppointmentsService/Appointments/Appointments.Application/{Events,Messages}`. A field added
there and not here is silently absent after deserialisation, not an error.

**`Dentists.Contracts.Events` / `Dentists.Contracts.Messages`** — owned by this service.
`Events` are published facts anyone may subscribe to. `Messages` are commands sent to a known
endpoint, and are internal to the dentist-assignment workflow.

## Intended direction

This project is the shared contracts package in embryo. Today it lives in the Dentists repo
and Appointments keeps its own copies. The next step is to publish it as a NuGet package that
both services reference, at which point the duplication — and the risk of the two definitions
drifting apart — goes away.
