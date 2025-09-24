using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class MinimalOpcProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🎯 Starting MINIMAL OPC UA Server for BadTcpInternalError fix...");

            try
            {
                // Create application instance first
                var application = new ApplicationInstance
                {
                    ApplicationName = "Minimal Beverage Server",
                    ApplicationType = ApplicationType.Server
                };

                // Create the most minimal config possible
                var config = new ApplicationConfiguration
                {
                    ApplicationName = "Minimal Beverage Server",
                    ApplicationUri = "urn:MinimalBeverageServer",
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
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection
                        {
                            new UserTokenPolicy(UserTokenType.Anonymous)
                        },
                        MaxSessionCount = 1,
                        MaxSessionTimeout = 30000,
                        MinRequestThreadCount = 1,
                        MaxRequestThreadCount = 1,
                        MaxQueuedRequestCount = 10
                    },

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
                        RejectUnknownRevocationStatus = false,
                        SuppressNonceValidationErrors = true,
                        AddAppCertToTrustedStore = true,
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OPC Foundation", "pki", "own"),
                            SubjectName = "CN=Minimal Beverage Server, O=MinimalSystems, DC=localhost"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OPC Foundation", "pki", "issuer")
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OPC Foundation", "pki", "trusted")
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OPC Foundation", "pki", "rejected")
                        }
                    },

                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 10000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 300000,
                        SecurityTokenLifetime = 3600000
                    }
                };

                application.ApplicationConfiguration = config;

                Console.WriteLine("🔧 Preparing certificate directories...");

                // Ensure certificate directories exist
                string pkiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OPC Foundation", "pki");
                Directory.CreateDirectory(Path.Combine(pkiPath, "own"));
                Directory.CreateDirectory(Path.Combine(pkiPath, "trusted"));
                Directory.CreateDirectory(Path.Combine(pkiPath, "issuer"));
                Directory.CreateDirectory(Path.Combine(pkiPath, "rejected"));
                Console.WriteLine($"📁 Certificate directories: {pkiPath}");

                Console.WriteLine("🔧 Checking certificates...");

                // Handle certificates properly
                try
                {
                    bool certOK = await application.CheckApplicationInstanceCertificates(false, 0);
                    if (!certOK)
                    {
                        Console.WriteLine("⚠️  Creating new certificate...");
                        await application.CheckApplicationInstanceCertificates(true, 2048);
                    }
                }
                catch (Exception certEx)
                {
                    Console.WriteLine($"⚠️  Certificate issue: {certEx.Message}");
                    Console.WriteLine("🔄 Forcing certificate creation...");
                    await application.CheckApplicationInstanceCertificates(true, 2048);
                }

                Console.WriteLine("✅ Certificate ready");
                Console.WriteLine("🚀 Starting server...");

                // Create minimal server
                var server = new MinimalOpcServer();

                // Start the server
                await application.Start(server);

                Console.WriteLine("🎉 OPC UA Server started successfully!");
                Console.WriteLine();
                Console.WriteLine("📡 Connection Details:");
                Console.WriteLine("   Endpoint: opc.tcp://localhost:4840");
                Console.WriteLine("   Security Policy: None");
                Console.WriteLine("   Message Mode: None");
                Console.WriteLine("   Authentication: Anonymous");
                Console.WriteLine();
                Console.WriteLine("🔍 In UaExpert:");
                Console.WriteLine("   1. Add Server → Add → opc.tcp://localhost:4840");
                Console.WriteLine("   2. Right-click → Connect");
                Console.WriteLine("   3. Browse 'Objects' folder");
                Console.WriteLine();

                // Keep running
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();

                Console.WriteLine("🛑 Stopping server...");
                server.Stop();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CRITICAL ERROR: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Type: {ex.InnerException.GetType().Name}");
                }

                Console.WriteLine();
                Console.WriteLine("🔍 Full Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
            }
        }
    }

    public class MinimalOpcServer : StandardServer
    {
        private BeverageFillingLineMachine m_machine;
        private Timer m_simulationTimer;

        public MinimalOpcServer()
        {
            m_machine = new BeverageFillingLineMachine();
        }

        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "Minimal Systems",
                ProductName = "Minimal Beverage Server",
                ProductUri = "urn:MinimalSystems:BeverageServer",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("🏗️  Creating MINIMAL master node manager...");

            try
            {
                var nodeManager = new MinimalBeverageNodeManager(server, configuration, m_machine);
                var masterManager = new MasterNodeManager(server, configuration, null, nodeManager);

                // Start simulation after node manager is ready
                m_simulationTimer = new Timer(UpdateSimulation, null, 2000, 3000);
                Console.WriteLine("✅ Minimal node manager created successfully");

                return masterManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Node manager creation failed: {ex.Message}");
                throw; // Re-throw to see full error
            }
        }

        private void UpdateSimulation(object state)
        {
            try
            {
                m_machine.UpdateSimulation();
                Console.WriteLine($"📊 [{DateTime.Now:HH:mm:ss}] " +
                                $"Status: {m_machine.MachineStatus} | " +
                                $"Fill: {m_machine.ActualFillVolume:F1}ml | " +
                                $"Tank: {m_machine.ProductLevelTank:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Simulation error: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_simulationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class MinimalBeverageNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;
        private Timer m_updateTimer;

        public MinimalBeverageNodeManager(IServerInternal server, ApplicationConfiguration configuration, BeverageFillingLineMachine machine)
            : base(server, configuration, "urn:MinimalBeverageServer:")
        {
            m_machine = machine;
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("urn:MinimalBeverageServer:");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Start updating OPC variables
                m_updateTimer = new Timer(UpdateOpcVariables, null, 2500, 3000);
                Console.WriteLine("📋 Address space created and variable updates started");
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            try
            {
                // Create root folder
                FolderState root = new FolderState(null)
                {
                    NodeId = new NodeId("BeverageFillingLine", NamespaceIndex),
                    BrowseName = new QualifiedName("BeverageFillingLine", NamespaceIndex),
                    DisplayName = new LocalizedText("Beverage Filling Line"),
                    TypeDefinitionId = ObjectTypeIds.FolderType
                };

                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                AddPredefinedNode(context, root);
                predefinedNodes.Add(root);

                // Create key variables only
                CreateVariable(root, "MachineStatus", DataTypeIds.String, m_machine.MachineStatus, predefinedNodes);
                CreateVariable(root, "ActualFillVolume", DataTypeIds.Double, m_machine.ActualFillVolume, predefinedNodes);
                CreateVariable(root, "TargetFillVolume", DataTypeIds.Double, m_machine.TargetFillVolume, predefinedNodes);
                CreateVariable(root, "ActualLineSpeed", DataTypeIds.Double, m_machine.ActualLineSpeed, predefinedNodes);
                CreateVariable(root, "ProductLevelTank", DataTypeIds.Double, m_machine.ProductLevelTank, predefinedNodes);
                CreateVariable(root, "CurrentStation", DataTypeIds.String, m_machine.CurrentStation, predefinedNodes);
                CreateVariable(root, "GoodBottles", DataTypeIds.UInt32, m_machine.GoodBottles, predefinedNodes);
                CreateVariable(root, "TotalBottles", DataTypeIds.UInt32, m_machine.TotalBottles, predefinedNodes);

                Console.WriteLine($"✅ Created {m_variables.Count} OPC UA variables");
                return predefinedNodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating address space: {ex.Message}");
                throw;
            }
        }

        private void CreateVariable(FolderState parent, string name, NodeId dataType, object initialValue, NodeStateCollection predefinedNodes)
        {
            try
            {
                var variable = new BaseDataVariableState(parent)
                {
                    NodeId = new NodeId(name, NamespaceIndex),
                    BrowseName = new QualifiedName(name, NamespaceIndex),
                    DisplayName = new LocalizedText(name),
                    TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                    DataType = dataType,
                    ValueRank = ValueRanks.Scalar,
                    AccessLevel = AccessLevels.CurrentRead,
                    UserAccessLevel = AccessLevels.CurrentRead,
                    Value = initialValue,
                    StatusCode = StatusCodes.Good,
                    Timestamp = DateTime.UtcNow
                };

                parent.AddChild(variable);
                predefinedNodes.Add(variable);
                m_variables[name] = variable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to create variable {name}: {ex.Message}");
                throw;
            }
        }

        private void UpdateOpcVariables(object state)
        {
            try
            {
                lock (Lock)
                {
                    UpdateVariable("MachineStatus", m_machine.MachineStatus);
                    UpdateVariable("ActualFillVolume", m_machine.ActualFillVolume);
                    UpdateVariable("TargetFillVolume", m_machine.TargetFillVolume);
                    UpdateVariable("ActualLineSpeed", m_machine.ActualLineSpeed);
                    UpdateVariable("ProductLevelTank", m_machine.ProductLevelTank);
                    UpdateVariable("CurrentStation", m_machine.CurrentStation);
                    UpdateVariable("GoodBottles", m_machine.GoodBottles);
                    UpdateVariable("TotalBottles", m_machine.TotalBottles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OPC variable update error: {ex.Message}");
            }
        }

        private void UpdateVariable(string name, object value)
        {
            if (m_variables.ContainsKey(name))
            {
                m_variables[name].Value = value;
                m_variables[name].Timestamp = DateTime.UtcNow;
                m_variables[name].ClearChangeMasks(SystemContext, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}