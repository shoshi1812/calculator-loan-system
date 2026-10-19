using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Common;

public record ItemWithAvailability(Item Item, int AvailableCopies);
