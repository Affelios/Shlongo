using System.Reflection;

namespace Shlongo
{
    class MongrationContext(Assembly mongrationAssembly) : IMongrationContext
    {
        public Mongration[] Mongrations { get; } = [.. mongrationAssembly
            .GetTypes()
            .Where(x => x.BaseType == typeof(Mongration))
            .Select(x => (Mongration)Activator.CreateInstance(x)!)];
    }
}
