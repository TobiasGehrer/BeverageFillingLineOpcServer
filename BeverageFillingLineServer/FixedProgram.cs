using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class FixedProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔧 Starting FIXED Beverage Filling Line Server...");

            try
            {
                var application = new ApplicationInstance
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationType = ApplicationType.Server
                };

                // Enhanced configuration for better compatibility
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
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection
                        {
                            new UserTokenPolicy(UserTokenType.Anonymous)
                        },
                        DiagnosticsEnabled = true,
                        MaxSessionCount = 100,
                        MaxSessionTimeout = 300000,
                        MaxBrowseContinuationPoints = 10,
                        MaxQueryContinuationPoints = 10,
                        MaxHistoryContinuationPoints = 100,
                        MaxRequestAge = 600000,
                        MinRequestThreadCount = 5,
                        MaxRequestThreadCount = 100,
                        MaxQueuedRequestCount = 200
                    },

                    SecurityConfiguration = new SecurityConfiguration
                    {
                        AutoAcceptUntrustedCertificates = true,
                        RejectSHA1SignedCertificates = false,
                        RejectUnknownRevocationStatus = false,
                        MinimumCertificateKeySize = 1024
                    },

                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas
                    {
                        OperationTimeout = 120000,
                        MaxStringLength = 1048576,
                        MaxByteStringLength = 1048576,
                        MaxArrayLength = 65535,
                        MaxMessageSize = 4194304,
                        MaxBufferSize = 65535,
                        ChannelLifetime = 300000,
                        SecurityTokenLifetime = 3600000
                    },

                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000,
                        MinSubscriptionLifetime = 10000
                    },

                    TraceConfiguration = new TraceConfiguration
                    {
                        OutputFilePath = "./Logs/BeverageServer.log",
                        TraceMasks = 0 // No tracing for cleaner output
                    }
                };

                application.ApplicationConfiguration = config;

                // Create enhanced server
                var server = new FixedServer();
                await application.Start(server);

                Console.WriteLine("✅ FIXED server started successfully!");
                Console.WriteLine($"🌐 OPC UA Endpoint: opc.tcp://localhost:4840");
                Console.WriteLine($"📊 Server URI: {config.ApplicationUri}");
                Console.WriteLine($"🔐 Security: None (Anonymous access)");
                Console.WriteLine();
                Console.WriteLine("🔍 Try connecting with UaExpert now!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                server.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ReadKey();
            }
        }
    }

    public class FixedServer : StandardServer
    {
        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "FluidFill Systems",
                ProductName = "FluidFill Express Simulator",
                ProductUri = "http://fluidfill.com/server/v1.0",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }

        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            Console.WriteLine("🏗️  Creating fixed node manager...");

            try
            {
                var nodeManager = new FixedBeverageNodeManager(server, configuration);
                var masterNodeManager = new MasterNodeManager(server, configuration, "http://fluidfill.com/dynamic/", nodeManager);
                Console.WriteLine("✅ Fixed node manager created successfully!");
                return masterNodeManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Node manager error: {ex.Message}");
                return new MasterNodeManager(server, configuration, "http://fluidfill.com/dynamic/");
            }
        }
    }

    public class FixedBeverageNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;
        private Timer m_updateTimer;

        public FixedBeverageNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, "http://fluidfill.com/beverage/")
        {
            m_machine = new BeverageFillingLineMachine();
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("http://fluidfill.com/beverage/");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);
                m_updateTimer = new Timer(UpdateVariables, null, 2000, 2000);
                Console.WriteLine("🗂️  Address space created with OPC UA variables");
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            // Create root folder
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("BeverageFillingLine", NamespaceIndex),
                BrowseName = new QualifiedName("Beverage Filling Line", NamespaceIndex),
                DisplayName = new LocalizedText("en", "Beverage Filling Line"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);

            // Create variables
            CreateVariable(root, "MachineStatus", DataTypeIds.String, m_machine.MachineStatus, predefinedNodes);
            CreateVariable(root, "ActualFillVolume", DataTypeIds.Double, m_machine.ActualFillVolume, predefinedNodes);
            CreateVariable(root, "TargetFillVolume", DataTypeIds.Double, m_machine.TargetFillVolume, predefinedNodes);
            CreateVariable(root, "ActualLineSpeed", DataTypeIds.Double, m_machine.ActualLineSpeed, predefinedNodes);
            CreateVariable(root, "ProductLevelTank", DataTypeIds.Double, m_machine.ProductLevelTank, predefinedNodes);
            CreateVariable(root, "CurrentStation", DataTypeIds.String, m_machine.CurrentStation, predefinedNodes);
            CreateVariable(root, "GoodBottles", DataTypeIds.UInt32, m_machine.GoodBottles, predefinedNodes);

            Console.WriteLine($"📋 Created {m_variables.Count} OPC UA variables");
            return predefinedNodes;
        }

        private void CreateVariable(FolderState parent, string name, NodeId dataType, object initialValue, NodeStateCollection predefinedNodes)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
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

        private void UpdateVariables(object state)
        {
            try
            {
                lock (Lock)
                {
                    m_machine.UpdateSimulation();

                    UpdateVariable("MachineStatus", m_machine.MachineStatus);
                    UpdateVariable("ActualFillVolume", m_machine.ActualFillVolume);
                    UpdateVariable("TargetFillVolume", m_machine.TargetFillVolume);
                    UpdateVariable("ActualLineSpeed", m_machine.ActualLineSpeed);
                    UpdateVariable("ProductLevelTank", m_machine.ProductLevelTank);
                    UpdateVariable("CurrentStation", m_machine.CurrentStation);
                    UpdateVariable("GoodBottles", m_machine.GoodBottles);

                    Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss}] Fill: {m_machine.ActualFillVolume:F1}ml | Tank: {m_machine.ProductLevelTank:F1}% | {m_machine.CurrentStation}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Update error: {ex.Message}");
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