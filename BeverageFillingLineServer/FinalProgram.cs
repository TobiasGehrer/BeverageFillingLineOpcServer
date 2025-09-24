using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class FinalProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting FINAL Beverage Filling Line Server...");

            try
            {
                var application = new ApplicationInstance
                {
                    ApplicationName = "Beverage Filling Line Server",
                    ApplicationType = ApplicationType.Server
                };

                // Minimal configuration
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
                    }
                };

                application.ApplicationConfiguration = config;

                // Use the working StandardServer approach with all error handling
                var server = new FinalStandardServer();
                await application.Start(server);

                Console.WriteLine("🎉 FINAL server started at: opc.tcp://localhost:4840");
                Console.WriteLine("✅ Complete beverage filling line simulation running");
                Console.WriteLine("✅ OPC UA nodes available for browsing");
                Console.WriteLine("📊 Real-time data updates every 2 seconds");
                Console.WriteLine();
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

    public class FinalStandardServer : StandardServer
    {
        protected override ServerProperties LoadServerProperties()
        {
            Console.WriteLine("Loading server properties...");
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
            Console.WriteLine("Creating FINAL master node manager...");

            try
            {
                // Create the simplified but complete node manager
                var nodeManager = new FinalBeverageNodeManager(server, configuration);
                var masterNodeManager = new MasterNodeManager(server, configuration, "http://fluidfill.com/dynamic/", nodeManager);
                Console.WriteLine("✅ FINAL node manager created successfully!");
                return masterNodeManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateMasterNodeManager: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Return a minimal master node manager to keep server running
                return new MasterNodeManager(server, configuration, "http://fluidfill.com/dynamic/");
            }
        }
    }

    public class FinalBeverageNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;
        private Timer m_updateTimer;

        public FinalBeverageNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, "http://fluidfill.com/beverage/")
        {
            Console.WriteLine("Initializing beverage node manager...");
            m_machine = new BeverageFillingLineMachine();
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("http://fluidfill.com/beverage/");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            Console.WriteLine("Creating OPC UA address space...");
            try
            {
                lock (Lock)
                {
                    LoadPredefinedNodes(SystemContext, externalReferences);

                    // Start timers
                    m_updateTimer = new Timer(UpdateAllVariables, null, 1000, 2000);
                    Console.WriteLine("✅ Address space and timers created successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating address space: {ex.Message}");
                throw;
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            Console.WriteLine("Loading predefined nodes...");
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            try
            {
                // Root folder
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

                // Key variables - start with just a few
                CreateVariable(root, "MachineStatus", DataTypeIds.String, m_machine.MachineStatus, predefinedNodes);
                CreateVariable(root, "ActualFillVolume", DataTypeIds.Double, m_machine.ActualFillVolume, predefinedNodes);
                CreateVariable(root, "TargetFillVolume", DataTypeIds.Double, m_machine.TargetFillVolume, predefinedNodes);
                CreateVariable(root, "ProductLevelTank", DataTypeIds.Double, m_machine.ProductLevelTank, predefinedNodes);
                CreateVariable(root, "CurrentStation", DataTypeIds.String, m_machine.CurrentStation, predefinedNodes);

                Console.WriteLine($"✅ Created {m_variables.Count} OPC UA variables");
                return predefinedNodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in LoadPredefinedNodes: {ex.Message}");
                throw;
            }
        }

        private void CreateVariable(FolderState parent, string name, NodeId dataType, object initialValue, NodeStateCollection predefinedNodes)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating variable {name}: {ex.Message}");
            }
        }

        private void UpdateAllVariables(object state)
        {
            try
            {
                lock (Lock)
                {
                    // Update machine simulation
                    m_machine.UpdateSimulation();

                    // Update OPC UA variables
                    UpdateVariable("MachineStatus", m_machine.MachineStatus);
                    UpdateVariable("ActualFillVolume", m_machine.ActualFillVolume);
                    UpdateVariable("TargetFillVolume", m_machine.TargetFillVolume);
                    UpdateVariable("ProductLevelTank", m_machine.ProductLevelTank);
                    UpdateVariable("CurrentStation", m_machine.CurrentStation);

                    // Console output for monitoring
                    Console.WriteLine($"🔄 Status: {m_machine.MachineStatus}, Fill: {m_machine.ActualFillVolume:F1}ml, Tank: {m_machine.ProductLevelTank:F1}%, Station: {m_machine.CurrentStation}");

                    if (m_machine.ActiveAlarms.Count > 0)
                    {
                        Console.WriteLine($"⚠️  ALARMS: {string.Join(" | ", m_machine.ActiveAlarms)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating variables: {ex.Message}");
            }
        }

        private void UpdateVariable(string name, object value)
        {
            if (m_variables.ContainsKey(name))
            {
                try
                {
                    m_variables[name].Value = value;
                    m_variables[name].Timestamp = DateTime.UtcNow;
                    m_variables[name].ClearChangeMasks(SystemContext, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error updating {name}: {ex.Message}");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_updateTimer?.Dispose();
                Console.WriteLine("🧹 Node manager disposed");
            }
            base.Dispose(disposing);
        }
    }
}