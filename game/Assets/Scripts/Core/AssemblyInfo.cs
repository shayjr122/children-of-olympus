using System.Runtime.CompilerServices;

// Tests may set tuning values on Config assets through internal setters; gameplay assemblies cannot (AD-5: SOs are
// read-only at runtime).
[assembly: InternalsVisibleTo("GMR.Tests.EditMode")]
[assembly: InternalsVisibleTo("GMR.Tests.PlayMode")]
