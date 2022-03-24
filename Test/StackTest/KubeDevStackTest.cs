using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Pulumi.Kubernetes.Apps.V1;
using Pulumi.Kubernetes.Core.V1;

namespace PulumiPoc.Test.StackTest
{
    [TestFixture]
    public class KubeDevStackTest
    {
        [Test]
        public async Task ExpectedTwoService_Successfully()
        {
            var resources = await TestingExtensions.RunStack<KubeDevStack>();

            var resourceGroups = resources.OfType<Service>().ToList();
            resourceGroups.Count.Should().Be(2, "a few services is expected.");

            var arrayTask = resourceGroups.Select(x => x.Metadata.GetValueAsync());
            var service = await Task.WhenAll(arrayTask);
            var serviceNames = service.Select(x => x.Name);

            CollectionAssert.Contains(serviceNames, "kafka-service");
            CollectionAssert.Contains(serviceNames, "zoo1");
        }
        
        [Test]
        public async Task ExpectedTwoDeployment_Successfully()
        {
            var resources = await TestingExtensions.RunStack<KubeDevStack>();

            var resourceGroups = resources.OfType<Deployment>().ToList();
            resourceGroups.Count.Should().Be(2, "a few deployments is expected.");

            var arrayTask = resourceGroups.Select(x => x.Metadata.GetValueAsync());
            var service = await Task.WhenAll(arrayTask);
            var serviceNames = service.Select(x => x.Name);

            CollectionAssert.Contains(serviceNames, "zookeeper-deploy");
            CollectionAssert.Contains(serviceNames, "kafka-broker");
        }
    }
}