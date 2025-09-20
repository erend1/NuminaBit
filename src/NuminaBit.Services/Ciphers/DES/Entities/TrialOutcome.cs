namespace NuminaBit.Services.Ciphers.DES.Entities
{
    /// <summary>
    /// Outcome of a single trial (RunAlgorithm1SingleAsync)
    /// </summary>
    public sealed record TrialOutcome
    {
        public Guid TrailId { get; } = Guid.NewGuid();
        public int Pairs { get; init; }
        public int CountZero { get; init; }
        public int CountOne { get; init; }
        public int GuessedBit { get; init; }
        public int ActualBit { get; init; }
        public bool Success { get; init; }
        public double SuccessAsDouble => Success ? 1.0 : 0.0;
    }
}
