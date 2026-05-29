namespace AcoTestApi.Domain.Enums;

public enum PrinterErrorState
{
    None = 0,
    PAPER_OUT = 1,
    PAPER_JAM = 2,
    COVER_OPEN = 3,
    OVERHEAT = 4,
    COMM_ERROR = 5,
    UNKNOWN_COMMAND = 6
}
