package net.ijbrown.bgtools.vifview;

import com.google.devtools.common.options.Option;
import com.google.devtools.common.options.OptionsBase;

/**
 * Command-line options.
 */
public class CliOptions extends OptionsBase {

    @Option(
            name = "help",
            abbrev = 'h',
            help = "Prints usage info.",
            defaultValue = "false"
    )
    public boolean help;

    @Option(
            name = "dir",
            abbrev = 'd',
            help = "Name of directory where the game lives (where the SYSTEM.CNF file lives).",
            defaultValue = ""
    )
    public String dir;

    @Option(
            name = "character",
            abbrev = 'c',
            help = "Name of the character to display.",
            defaultValue = ""
    )
    public String character;

    @Option(
            name = "export",
            abbrev = 'e',
            help = "Export the character to the given filename (.gltf).",
            defaultValue = ""
    )
    public String exportFilename;

}
