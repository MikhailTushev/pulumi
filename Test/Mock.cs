using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi.Testing;

namespace PulumiPoc.Test
{
    public class Mocks : IMocks
    {
        public Task<(string? id, object state)> NewResourceAsync(MockResourceArgs args)
        {
            var outputs = ImmutableDictionary.CreateBuilder<string, object>();

            outputs.AddRange(args.Inputs);

            args.Id ??= $"{args.Name}_id";
            return Task.FromResult((args?.Id, (object) outputs));
        }

        public Task<object> CallAsync(MockCallArgs args)
        {
            return Task.FromResult((object) args);
        }
    }
}