using System.Collections.Immutable;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.Testing;

namespace PulumiPoc.Test
{
    public static class TestingExtensions
    {
        public static Task<ImmutableArray<Resource>> RunStack<T>()
            where T : Stack, new()
        {
            return Deployment.TestAsync<T>(new Mocks(), new TestOptions {IsPreview = false});
        }

        public static Task<T> GetValueAsync<T>(this Output<T> output)
        {
            var tcs = new TaskCompletionSource<T>();
            output.Apply(v =>
            {
                tcs.SetResult(v);
                return v;
            });
            return tcs.Task;
        }
    }
}