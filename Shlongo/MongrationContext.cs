using System.Reflection;

namespace Shlongo
{
    class MongrationContext(Assembly mongrationAssembly) : IMongrationContext
    {
        public Mongration[] Mongrations { get; } = [.. mongrationAssembly
            .GetTypes()
            .Where(x => x.BaseType == typeof(Mongration))
            .Select(x => (Mongration)Activator.CreateInstance(x)!)
            .OrderBy(x => int.Parse(x.GetType().Name.Split('_')[1]))];
    }
}
