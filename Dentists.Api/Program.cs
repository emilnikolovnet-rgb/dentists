using Appointments.Application.Messages;
using Dentists.Api.Extensions;
using Dentists.Application.Behaviors;
using Dentists.Application.Consumers;
using Dentists.Application.Messaging;
using Dentists.Application.Queries;
using Dentists.Application.Sagas;
using Dentists.Domain.Repositories;
using Dentists.Infrastructure.Messaging;
using Dentists.Infrastructure.Persistence;
using Dentists.Infrastructure.Repositories;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.

// Appointment statuses cross the wire as names. Without this they would be the enum's
// ordinals, which are meaningless to a caller and silently wrong if the enum ever gains a
// member in the middle.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseName = builder.Configuration["Cosmos:DatabaseName"];

builder.Services.AddDbContext<DentistsDbContext>(options =>
    options.UseCosmos(connectionString!, databaseName!));

// Add Unit of Work. The repository is reached through it rather than injected, so it needs no
// registration of its own.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---- Messaging -----------------------------------------------------------------------

var serviceBusConnectionString = builder.Configuration["SERVICE_BUS_CONNECTION_STRING"];

// The saga is keyed by appointment rather than by dentist, so it needs a container of its own
// alongside the dentists.
const string SagaContainerName = "dentist-assignment-saga";

// MassTransit's Cosmos repository wants the endpoint and key apart, EF wants them together.
var cosmosAccount = CosmosConnectionString.Parse(connectionString!);

builder.Services.Configure<DentistAssignmentOptions>(
    builder.Configuration.GetSection(DentistAssignmentOptions.SectionName));
builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection(OutboxOptions.SectionName));

// The saga sends these rather than publishing them, so each needs a destination. The names
// match the kebab-case endpoints of the consumers that serve them in the Appointments service.
EndpointConvention.Map<ConfirmAppointmentRequested>(new Uri("queue:confirm-appointment-requested"));
EndpointConvention.Map<CancelAppointment>(new Uri("queue:cancel-appointment"));

// Pinned to MassTransit 8.x on purpose: 9 refuses to start the bus without a commercial
// licence. 8.5.10 is the last Apache-2.0 release. The envelope format is unchanged between the
// two, so this still interoperates with services running 9.
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReserveDentistConsumer>();
    x.AddConsumer<ReleaseDentistConsumer>();
    x.AddConsumer<SetDentistAppointmentStatusConsumer>();
    x.AddConsumer<RescheduleDentistAppointmentConsumer>();

    // Saga state is keyed by appointment, not by dentist, so it cannot live in a dentist
    // document and gets a container of its own. Optimistic concurrency because that is what
    // Cosmos offers — the pessimistic row lock the Appointments service uses has no equivalent.
    x.AddSagaStateMachine<DentistAssignmentStateMachine, DentistAssignmentState>()
        .CosmosRepository(r =>
        {
            r.AccountEndpoint = cosmosAccount.AccountEndpoint;
            r.AuthKeyOrResourceToken = cosmosAccount.AccountKey;
            r.DatabaseId = databaseName!;
            r.CollectionId = SagaContainerName;
        });

    x.SetKebabCaseEndpointNameFormatter();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(serviceBusConnectionString);
        cfg.ConfigureEndpoints(context);
    });
});

// Turns a contract into a queued outbox payload. Registered against the interface the
// consumers depend on, so they stay unaware of how it is serialised.
builder.Services.AddScoped<IOutboxEnqueuer, OutboxMessageSerializer>();

// Publishes what the consumers queued. Nothing they write reaches the transport without it.
builder.Services.AddHostedService<OutboxDispatcher>();

// Add MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetAllDentistsQuery).Assembly));

// Add FluentValidation, executed for every request through the MediatR pipeline
builder.Services.AddValidatorsFromAssembly(typeof(GetAllDentistsQuery).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Keep the built-in messages in English rather than following the server's culture.
ValidatorOptions.Global.LanguageManager.Enabled = false;

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Cosmos has no migrations: the database and its containers have to exist before the
    // first query. Provisioning outside the app is the expectation everywhere else.
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DentistsDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
