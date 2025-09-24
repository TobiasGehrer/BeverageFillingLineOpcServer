using Opc.Ua;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class SimpleNodeManager : CustomNodeManager2
    {
        private Dictionary<string, BaseDataVariableState> m_variables;

        public SimpleNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, "http://simple.com/test/")
        {
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("http://simple.com/test/");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();

            // Create a simple root folder
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("TestFolder", NamespaceIndex),
                BrowseName = new QualifiedName("Test Folder", NamespaceIndex),
                DisplayName = new LocalizedText("en", "Test Folder"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);

            // Create a simple variable
            BaseDataVariableState variable = new BaseDataVariableState(root)
            {
                NodeId = new NodeId("TestVariable", NamespaceIndex),
                BrowseName = new QualifiedName("Test Variable", NamespaceIndex),
                DisplayName = new LocalizedText("en", "Test Variable"),
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                DataType = DataTypeIds.String,
                ValueRank = ValueRanks.Scalar,
                Value = "Hello World",
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            root.AddChild(variable);
            predefinedNodes.Add(variable);
            m_variables["TestVariable"] = variable;

            return predefinedNodes;
        }
    }
}