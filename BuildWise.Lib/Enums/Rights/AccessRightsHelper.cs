using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BuildWise.Lib.Enums.Rights
{
    public static class AccessRightsHelper
    {
        public static IEnumerable<IGrouping<string, AccessRightOption>> GetGroups()
        {
            var options = new List<AccessRightOption>();

            foreach (AccessRights right in Enum.GetValues(typeof(AccessRights)))
            {
                var value = (int)right;
                if (value <= 0 || (value & (value - 1)) != 0)
                {
                    continue;
                }

                var field = typeof(AccessRights).GetField(right.ToString())!;
                var name = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? right.ToString();
                var group = field.GetCustomAttribute<CategoryAttribute>()?.Category ?? "Other";

                options.Add(new AccessRightOption(right, name, group));
            }

            return options.GroupBy(option => option.Group);
        }
    }
}
