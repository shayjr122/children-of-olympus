namespace GMR.Core.Status
{
    /// <summary>What re-applying a status the target already carries does (FR-18).</summary>
    public enum RefreshRule
    {
        /// <summary>Remaining duration resets to the full duration; stacks unchanged.</summary>
        RefreshDuration = 0,

        /// <summary>Adds the applied stacks (up to maxStacks) and resets the duration.</summary>
        AddStacks = 1,

        /// <summary>Nothing changes (the first application keeps running); behaviours still react to the application.</summary>
        KeepExisting = 2,
    }
}
