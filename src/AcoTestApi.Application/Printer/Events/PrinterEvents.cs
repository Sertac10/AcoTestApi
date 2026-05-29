using MediatR;
using AcoTestApi.Domain.Enums;

namespace AcoTestApi.Application.Printer.Events;

public record PrinterConnectionChangedEvent(ConnectionState State, PrinterMode Mode) : INotification;

public record PrinterHardwareErrorEvent(PrinterErrorState ErrorState) : INotification;
