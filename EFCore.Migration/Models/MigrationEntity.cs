using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.Migration;

/// <summary>
/// migration entity
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class MigrationEntity
{
    /// <summary>
    /// id
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(Order = 0)]
    public int Id { get; set; }

    /// <summary>
    ///
    /// </summary>
    public MigrationEntity() { }

    /// <summary>
    ///
    /// </summary>
    /// <param name="migrations"></param>
    /// <param name="name"></param>
    public MigrationEntity(byte[]? migrations, string? name)
    {
        Migrations = migrations;
        Name = name;
        MigrationTime = DateTime.Now;
    }

    /// <summary>
    /// migration buffer
    /// </summary>

    [Column(Order = 3)]
    [Required]
    public byte[]? Migrations { get; set; }

    /// <summary>
    /// migration name
    /// </summary>
    [Column(Order = 1)]
    [Required]
    [StringLength(128)]
    public string? Name { get; set; }

    /// <summary>
    /// migration time
    /// </summary>
    [Column(Order = 3)]
    public DateTime MigrationTime { get; set; }
}
