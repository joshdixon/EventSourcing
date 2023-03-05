using IdGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Common;

public static class Snowflake
{
    private static IdGen.IdGenerator? _generator;

    public static void Configure()
    {
        // Let's say we take jan 1st 2023 as our epoch
        var epoch = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local);

        // Create an ID with 45 bits for timestamp, 2 for generator-id
        // and 16 for sequence
        var structure = new IdStructure(45, 2, 16);

        // Prepare options
        var options = new IdGeneratorOptions(structure, new DefaultTimeSource(epoch));

        // Create an IdGenerator with it's generator-id set to 0, our custom epoch
        // and id-structure
        _generator = new IdGen.IdGenerator(0, options);
    }

    public static long NewId() => _generator?.CreateId() ?? throw new Exception("IdGenerator not initialised.");
}

