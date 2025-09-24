using Opc.Ua;

namespace BeverageFillingLineServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Beverage Filling Line Server...");          

            try
            {
                // Create application instance
                ApplicationInstance application = new ApplicationInstance
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationType = ApplicationType.Server,
                    ConfigSectionName = "BeverageFillingLineServer"
                };

                // Create server configuration
                var config = CreateServerConfiguration();
                
                // Process the command line and load the application configuration.
                bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
                
                // Create the server
                var server = new BeverageFillingLineServer();
                
                // Start the server
                await application.Start(server);

                Console.WriteLine("Server started at: opc.tcp://localhost:4840");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                // Stop the server
                server.Stop();
            }
            catch (ServiceResultException ex)
            {
                Console.WriteLine($"OPC UA Service Error: {ex.Message}");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"Inner Result: {ex.InnerResult}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static ApplicationConfiguration CreateServerConfiguration()
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "Beverage Filling Line Server",
                ApplicationUri = "urn:localhost:BeverageFillingLineServer",
                ApplicationType = ApplicationType.Server,

                ServerConfiguration = new ServerConfiguration()
                {
                    BaseAddresses = new StringCollection { "opc.tcp://localhost:4840" },
                    SecurityPolicies = new ServerSecurityPolicyCollection()
                    {
                        new ServerSecurityPolicy()
                        {
                            SecurityMode = MessageSecurityMode.None,
                            SecurityPolicyUri = SecurityPolicies.None
                        }
                    },
                    UserTokenPolicies = new UserTokenPolicyCollection()
                    {
                        new UserTokenPolicy(UserTokenType.Anonymous)                       
                    }
                },

                SecurityConfiguration = new SecurityConfiguration
                {
                    AutoAcceptUntrustedCertificates = true
                },
                
                TraceConfiguration = new TraceConfiguration()
            };

            // This is critical - StandardServer requires a certificate validator
            config.CertificateValidator = new CertificateValidator();

            return config;
        }
    }
}
