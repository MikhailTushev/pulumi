using System.Threading.Tasks;
using Pulumi;

class Program
{
    static Task<int> Main(string[] args)
    {
        //create builder here.
        //Deployment.RunAsync<DataBaseStack>()

        return Deployment.RunAsync<KubeDevStack>();
    }
}