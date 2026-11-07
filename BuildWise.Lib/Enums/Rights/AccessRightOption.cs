namespace BuildWise.Lib.Enums.Rights
{
    public sealed class AccessRightOption
    {
        public AccessRightOption(AccessRights value, string name, string group)
        {
            Value = value;
            Name = name;
            Group = group;
        }

        public AccessRights Value { get; }

        public string Name { get; }

        public string Group { get; }
    }
}
