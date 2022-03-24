using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Apps.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Deployment = Pulumi.Kubernetes.Apps.V1.Deployment;

public class DataBaseStack : Stack
{
    [Output("redis")]
    public Output<string> RedisName { get; set; }

    [Output("postgres")]
    public Output<string> PostgresName { get; set; }

    public DataBaseStack()
    {
        GetRedis();
        GetPosgres();
    }

    private void GetRedis()
    {
        var redisApp = new InputMap<string>
        {
            { "app", "redis-app" },
        };

        _ = new PersistentVolumeClaim("redis-data", new PersistentVolumeClaimArgs()
        {
            Kind = "PersistentVolumeClaim",
            Metadata = new ObjectMetaArgs()
            {
                Name = "redis-data",
                Labels = redisApp,
            },
            Spec = new PersistentVolumeClaimSpecArgs()
            {
                AccessModes = new InputList<string>()
                {
                    "ReadWriteMany",
                },
                Resources = new ResourceRequirementsArgs()
                {
                    Requests = new InputMap<string>()
                    {
                        { "storage", "100Mi" }
                    },
                },
            },
        });

        _ = new Deployment("redis_bd", new DeploymentArgs()
        {
            Kind = "Deployment",
            Metadata = new ObjectMetaArgs()
            {
                Name = "redis",
            },
            Spec = new DeploymentSpecArgs()
            {
                Selector = new LabelSelectorArgs()
                {
                    MatchLabels = redisApp
                },
                Replicas = 1,
                Template = new PodTemplateSpecArgs()
                {
                    Metadata = new ObjectMetaArgs()
                    {
                        Labels = redisApp,
                    },
                    Spec = new PodSpecArgs()
                    {
                        Containers = new ContainerArgs()
                        {
                            Name = "redis",
                            Image = "redis:latest",
                            ImagePullPolicy = "IfNotPresent",
                            Ports = new ContainerPortArgs()
                            {
                                ContainerPortValue = 6379
                            },
                            VolumeMounts = new VolumeMountArgs()
                            {
                                Name = "data",
                                MountPath = "/data",
                                ReadOnly = false,
                            }
                        },
                        Volumes = new VolumeArgs()
                        {
                            Name = "data",
                            PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceArgs()
                            {
                                ClaimName = "redis-data"
                            }
                        },
                    },
                },
            },
        });

        var service = new Service("redis", new ServiceArgs()
        {
            Kind = "Service",
            Metadata = new ObjectMetaArgs
            {
                Name = "redis",
                Labels = redisApp,
            },
            Spec = new ServiceSpecArgs()
            {
                Type = ServiceSpecType.NodePort,
                Selector = redisApp,
                Ports = new InputList<ServicePortArgs>()
                {
                    new ServicePortArgs()
                    {
                        Port = 6379,
                        TargetPort = 6379,
                    },
                },
            },
        });

        this.RedisName = service.Metadata.Apply(m => m.Name);
    }

    private void GetPosgres()
    {
        var postgresLabels = new InputMap<string>
        {
            { "POSTGRES-DB", "postgresdb" },
            { "POSTGRES-USER", "postgresadmin" },
            { "POSTGRES-PASSWORD", "admin123" },
            { "ALLOWED_HOSTS", "*" },
        };

        var postgresLb = new InputMap<string>
        {
            { "app", "postgres" },
        };

        _ = new ConfigMap("postgres-config", new ConfigMapArgs()
        {
            Kind = "ConfigMap",
            Metadata = new ObjectMetaArgs()
            {
                Name = "postgres-config",
                Labels = postgresLb,
            },
            Data = postgresLabels,
        });

        _ = new PersistentVolume("postgres-pv-volume", new PersistentVolumeArgs()
        {
            Kind = "PersistentVolume",
            Metadata = new ObjectMetaArgs()
            {
                Name = "postgres-pv-volume",
                Labels = new InputMap<string>
                {
                    postgresLb,
                    { "type", "local" },
                },
            },
            Spec = new PersistentVolumeSpecArgs()
            {
                StorageClassName = "manual",
                Capacity = new InputMap<string>()
                {
                    { "storage", "5Gi" }
                },
                AccessModes = new InputList<string>()
                {
                    "ReadWriteMany",
                },
                HostPath = new HostPathVolumeSourceArgs()
                {
                    Path = "/mnt/data",
                }
            }
        });

        _ = new PersistentVolumeClaim("postgres-pv-claim", new PersistentVolumeClaimArgs()
        {
            Kind = "PersistentVolumeClaim",
            Metadata = new ObjectMetaArgs()
            {
                Name = "postgres-pv-claim",
                Labels = postgresLb,
            },
            Spec = new PersistentVolumeClaimSpecArgs()
            {
                StorageClassName = "manual",
                AccessModes = new InputList<string>()
                {
                    "ReadWriteMany",
                },
                Resources = new ResourceRequirementsArgs()
                {
                    Requests = new InputMap<string>()
                    {
                        { "storage", "5Gi" }
                    },
                },
            },
        });

        _ = new Deployment("postgres", new DeploymentArgs()
        {
            Kind = "Deployment",
            Metadata = new ObjectMetaArgs()
            {
                Name = "postgres"
            },
            Spec = new DeploymentSpecArgs()
            {
                Selector = new LabelSelectorArgs
                {
                    MatchLabels = postgresLb
                },

                Replicas = 1,
                Template = new PodTemplateSpecArgs()
                {
                    Metadata = new ObjectMetaArgs()
                    {
                        Labels = postgresLb,
                    },
                    Spec = new PodSpecArgs()
                    {
                        Containers = new ContainerArgs()
                        {
                            Name = "postgres",
                            Image = "postgres:10.4",
                            ImagePullPolicy = "IfNotPresent",
                            Ports = new ContainerPortArgs()
                            {
                                ContainerPortValue = 5432,
                            },
                            EnvFrom = new EnvFromSourceArgs()
                            {
                                ConfigMapRef = new ConfigMapEnvSourceArgs()
                                {
                                    Name = "postgres-config"
                                }
                            },
                            VolumeMounts = new VolumeMountArgs()
                            {
                                MountPath = "/var/lib/postgresql/data",
                                Name = "postgresdb"
                            }
                        },
                        Volumes = new VolumeArgs()
                        {
                            Name = "postgresdb",
                            PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceArgs()
                            {
                                ClaimName = "postgres-pv-claim",
                            }
                        }
                    },
                }
            }
        });

        var service = new Service("postgres", new ServiceArgs()
        {
            Kind = "Service",
            Metadata = new ObjectMetaArgs
            {
                Name = "postgres",
                Labels = postgresLb,
            },
            Spec = new ServiceSpecArgs()
            {
                Type = ServiceSpecType.NodePort,
                Selector = postgresLb,
                Ports = new InputList<ServicePortArgs>()
                {
                    new ServicePortArgs()
                    {
                        Port = 5432,
                        TargetPort = 5432,
                    },
                },
            },
        });

        this.PostgresName = service.Metadata.Apply(m => m.Name);
    }
}