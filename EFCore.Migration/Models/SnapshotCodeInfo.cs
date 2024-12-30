using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.Migration.Models;

internal record SnapshotCodeInfo(string Token, string Namespace, string ClassName)
{
    public string TypeName => $"{Namespace}.{ClassName}";
}
