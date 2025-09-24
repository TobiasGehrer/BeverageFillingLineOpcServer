using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Beverage Filling Line Server...");

            try
            {
                var application = new ApplicationInstance
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationType = ApplicationType.Server
                };

                var config = new ApplicationConfiguration
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationUri = "urn:localhost:BeverageFillingLineServer",
                    ApplicationType = ApplicationType.Server,

                    ServerConfiguration = new ServerConfiguration
                    {
                        BaseAddresses = new StringCollection { "opc.tcp://localhost:4840" },
                        SecurityPolicies = new ServerSecurityPolicyCollection
                        {
                            new ServerSecurityPolicy
                            {
                                SecurityMode = MessageSecurityMode.None,
                                SecurityPolicyUri = SecurityPolicies.None
                            }
                        }
                    },

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true
                    },

                    CertificateValidator = new CertificateValidator()
                };

                config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = true; };
                application.ApplicationConfiguration = config;

                // Create a simple server directly
                var server = new SimpleServer();
                await application.Start(server);

                Console.WriteLine("Server started at: opc.tcp://localhost:4840");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                server.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ReadKey();
            }
        }
    }

    public class SimpleServer : StandardServer
    {
        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "FluidFill Systems",
                ProductName = "FluidFill Express Simulator",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("Creating master node manager...");

            try
            {
                // Try with simple node manager first
                var nodeManager = new SimpleNodeManager(server, configuration);
                var masterNodeManager = new MasterNodeManager(server, configuration, null, nodeManager);
                Console.WriteLine("Simple node manager created successfully");
                return masterNodeManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating node manager: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}