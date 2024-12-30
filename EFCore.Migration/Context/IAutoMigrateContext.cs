using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFCore.Migration.Models;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Migration;

/// <summary>
/// migrate context
/// </summary>
public interface IMigrateContext
{
    /// <summary>
    /// migration entities
    /// </summary>
    DbSet<MigrationEntity> MigrationEntities { get; }
}
