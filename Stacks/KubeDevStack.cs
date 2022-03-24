using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Apps.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Deployment = Pulumi.Kubernetes.Apps.V1.Deployment;

public class KubeDevStack : DevStack
{
    [Output("kafka")]
    public Output<string> KafkaServiceName { get; set; }

    [Output("zoo")]
    public Output<string> ZookeeperServiceName { get; set; }

    public KubeDevStack()
    {
        GetKafka();
        GetZookeeper();
    }

    private void GetZookeeper()
    {
        var appLabels = new InputMap<string>
        {
            { "app", "zookeeper-1" },
        };

        Log.Info("start zoo-service");

        var zooDeploy = new Deployment("zookeeper-deploy", new DeploymentArgs
        {
            ApiVersion = "apps/v1",
            Metadata = new ObjectMetaArgs()
            {
                Name = "zookeeper-deploy",
            },
            Spec = new DeploymentSpecArgs
            {
                Selector = new LabelSelectorArgs
                {
                    MatchLabels = appLabels,
                },
                Replicas = 1,
                Template = new PodTemplateSpecArgs
                {
                    Metadata = new ObjectMetaArgs
                    {
                        Labels = appLabels,
                    },
                    Spec = new PodSpecArgs
                    {
                        Containers =
                        {
                            new ContainerArgs
                            {
                                Name = "zoo1",
                                Image = "wurstmeister/zookeeper",
                                Ports =
                                {
                                    new ContainerPortArgs
                                    {
                                        ContainerPortValue = 2181,
                                    },
                                },
                                Env = new InputList<EnvVarArgs>()
                                {
                                    new EnvVarArgs()
                                    {
                                        Name = "ZOOKEEPER_ID",
                                        Value = "1",
                                    },
                                    new EnvVarArgs()
                                    {
                                        Name = "ZOOKEEPER_SERVER_1",
                                        Value = "zoo1",
                                    },
                                }
                            },
                        },
                    },
                },
            },
        });

        Log.Info("finish zoo deploy | start zoo-service");

        var zooService = new Service("zoo", new ServiceArgs()
        {
            ApiVersion = "v1",
            Metadata = new ObjectMetaArgs
            {
                Name = "zoo1",
                Labels = appLabels,
            },
            Spec = new ServiceSpecArgs()
            {
                Selector = appLabels,
                Ports = new InputList<ServicePortArgs>()
                {
                    new ServicePortArgs()
                    {
                        Name = "client",
                        Port = 2181,
                        TargetPort = 2181,
                        Protocol = "TCP",
                    },
                    new ServicePortArgs()
                    {
                        Name = "follower",
                        Port = 2888,
                        TargetPort = 2888,
                        Protocol = "TCP",
                    },
                    new ServicePortArgs()
                    {
                        Name = "leader",
                        Port = 3888,
                        TargetPort = 3888,
                        Protocol = "TCP",
                    },
                },
            },
        });

        Log.Info("finish zoo-service");

        ZookeeperServiceName = zooService.Metadata.Apply(m => m.Name);
    }

    private void GetKafka()
    {
        var appLabels = new InputMap<string>
        {
            { "app", "kafka" },
            { "id", "0" },
        };

        Log.Info("start kafka deploy");

        var kafkaDeploy = new Deployment("kafka-deploy", new DeploymentArgs
        {
            ApiVersion = "apps/v1",
            Metadata = new ObjectMetaArgs()
            {
                Name = "kafka-broker",
                Labels = appLabels,
            },
            Spec = new DeploymentSpecArgs
            {
                Selector = new LabelSelectorArgs
                {
                    MatchLabels = appLabels,
                },
                Replicas = 1,
                Template = new PodTemplateSpecArgs
                {
                    Metadata = new ObjectMetaArgs
                    {
                        Labels = appLabels,
                    },
                    Spec = new PodSpecArgs
                    {
                        Containers =
                        {
                            new ContainerArgs
                            {
                                Name = "kafka",
                                Image = "wurstmeister/kafka",
                                Ports =
                                {
                                    new ContainerPortArgs
                                    {
                                        ContainerPortValue = 9092,
                                    },
                                },
                                Env = new InputList<EnvVarArgs>()
                                {
                                    new EnvVarArgs()
                                    {
                                        Name = "KAFKA_ZOOKEEPER_CONNECT",
                                        Value = "zoo1:2181",
                                    },
                                    new EnvVarArgs()
                                    {
                                        Name = "KAFKA_ADVERTISED_LISTENERS",
                                        Value = "PLAINTEXT://localhost:9092",
                                    },
                                    new EnvVarArgs()
                                    {
                                        Name = "KAFKA_LISTENERS",
                                        Value = "PLAINTEXT://0.0.0.0:9092",
                                    },
                                    new EnvVarArgs()
                                    {
                                        Name = "KAFKA_BROKER_ID",
                                        Value = "0",
                                    },
                                    new EnvVarArgs()
                                    {
                                        Name = "KAFKA_CREATE_TOPICS",
                                        Value = "simple.topic:1:1",
                                    },
                                }
                            },
                        },
                    },
                },
            },
        });

        Log.Info("finish kafka deploy | start kafka-service");

        var kafkaService = new Service("kafka-service", new ServiceArgs()
        {
            ApiVersion = "v1",
            Metadata = new ObjectMetaArgs
            {
                Name = "kafka-service",
                Labels = new InputMap<string>() { { "name", "kafka" } },
            },
            Spec = new ServiceSpecArgs()
            {
                Type = ServiceSpecType.NodePort,
                Selector = appLabels,
                Ports = new InputList<ServicePortArgs>()
                {
                    new ServicePortArgs()
                    {
                        Name = "kafka-port",
                        Port = 9092,
                        TargetPort = 9092,
                        Protocol = "TCP",
                    },
                },
            },
        });

        Log.Info("finish kafka-service");

        KafkaServiceName = kafkaService.Metadata.Apply(m => m.Name);
    }

    /*
    private Output<string> GetKafkaTopic()
    {
        var kafka = new Provider("kafka", new ProviderArgs()
        {
            BootstrapServers = "127.0.0.1:9092",
        });
        var topicsConfig = new InputMap<object>
        {
            { "segment.ms", "4000" },
            { "retention.ms", "86400000" },
        };

        var topic = new Topic("topic", new TopicArgs
        {
            Name = "my-topic",
            ReplicationFactor = 3,
            Partitions = 12,
            Config = topicsConfig,
        });
        return topic.Name;
    }

    private static IEnumerable<Pulumi.Docker.Container> DockerComposeRemoteImages()
    {
        var network = new Network("kafka-network", new NetworkArgs()
        {
            Driver = "bridge",
        });

        var zookeeper = new RemoteImage("zookeeper", new RemoteImageArgs()
        {
            Name = "wurstmeister/kafka:latest",
            KeepLocally = true,
        });

        var zookeeperContainer = new Pulumi.Docker.Container("zookeeperContainer", new Pulumi.Docker.ContainerArgs()
        {
            Image = zookeeper.Name,
            NetworksAdvanced = new InputList<ContainerNetworksAdvancedArgs>()
            {
                new ContainerNetworksAdvancedArgs() { Name = network.Name },
            },
            Restart = "on-failure",

            Ports = new InputList<Pulumi.Docker.Inputs.ContainerPortArgs>()
            {
                new Pulumi.Docker.Inputs.ContainerPortArgs()
                {
                    Internal = 2181,
                    External = 2181,
                },
            },
        });

        var kafka = new RemoteImage("kafka", new RemoteImageArgs()
        {
            Name = "wurstmeister/kafka:latest",
            KeepLocally = true,
        });

        var kafkaContainer = new Pulumi.Docker.Container("kafkaContainer", new Pulumi.Docker.ContainerArgs()
        {
            Image = kafka.Name,

            NetworksAdvanced = new InputList<ContainerNetworksAdvancedArgs>()
            {
                new ContainerNetworksAdvancedArgs() { Name = network.Name },
            },
            Restart = "on-failure",
            Ports = new InputList<Pulumi.Docker.Inputs.ContainerPortArgs>()
            {
                new Pulumi.Docker.Inputs.ContainerPortArgs()
                {
                    External = 9092,
                    Internal = 9092,
                }
            },
            Envs = new InputList<string>()
            {
                "KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://localhost:9092",
                "KAFKA_LISTENERS=PLAINTEXT://0.0.0.0:9092",
                zookeeperContainer.Name.Apply(name => $"KAFKA_ZOOKEEPER_CONNECT={name}:2181"),
            },
        });

        return new List<Pulumi.Docker.Container>()
        {
            zookeeperContainer,
            kafkaContainer,
        };
    }
    */
}