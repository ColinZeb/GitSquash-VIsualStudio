﻿namespace Git.VisualStudio
{
    /// <summary>
    /// A list of options when we are doing 
    /// </summary>
    public enum GitLogOptions
    {
        /// <summary>
        /// If there are no additional options for the log.
        /// </summary>
        None = 0,

        /// <summary>
        /// Order the log items in topological ordering.
        /// </summary>
        TopologicalOrder = 1,

        /// <summary>
        /// Include merges in the log entries. 
        /// </summary>
        IncludeMerges = 2,

        /// <summary>
        /// Include remote commits in the log entries.
        /// </summary>
        IncludeRemotes = 4,
    }
}
