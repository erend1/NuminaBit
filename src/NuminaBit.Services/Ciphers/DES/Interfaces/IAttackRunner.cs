using NuminaBit.Services.Ciphers.DES.Entities;

namespace NuminaBit.Services.Ciphers.DES.Interfaces
{
    public interface IAttackRunner
    {
        public ulong HiddenKey { get; }

        /// <summary>
        /// Run a single Algorithm-1 experiment: generate 'pairs' random plaintexts, compute lhs for each,
        /// then decide guessed bit by majority (0 if majority of lhs==0).
        /// Returns TrialOutcome with counts and actual bit (XOR of K1[22] and K3[22]).
        /// </summary>
        public Task<TrialOutcome> RunAlgorithm1On3RoundSingleAsync(ulong key64, int pairs);

        /// <summary>
        /// Run multiple independent trials and return their outcomes.
        /// </summary>
        public Task<List<TrialOutcome>> RunAlgorithm1OnRound3MultipleAsync(ulong key64, int pairsPerTrial, int trials);

        /// <summary>
        /// Run a single Algorithm-1 experiment: generate 'pairs' random plaintexts, compute lhs for each,
        /// then decide guessed bit by majority (0 if majority of lhs==0).
        /// Returns TrialOutcome with counts and actual bit (XOR of K1[22] and K3[22]).
        /// </summary>
        public Task<TrialOutcome> RunAlgorithm1On5RoundSingleAsync(ulong key64, int pairs);

        /// <summary>
        /// Run multiple independent trials and return their outcomes.
        /// </summary>
        public Task<List<TrialOutcome>> RunAlgorithm1OnRound5MultipleAsync(ulong key64, int pairsPerTrial, int trials);
    }
}
