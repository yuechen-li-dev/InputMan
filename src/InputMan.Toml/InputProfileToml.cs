using System.Globalization;
using System.Text;
using InputMan.Core;
using InputMan.Core.Validation;

namespace InputMan.Toml;

/// <summary>Canonical TOML serializer for the bounded InputMan format version 1 schema.</summary>
public static class InputProfileToml
{
    public const int CurrentFormatVersion = 1;

    public static string Save(InputProfile profile)
    {
        InputProfileValidator.Validate(profile);
        var text = new StringBuilder();
        text.AppendLine($"formatVersion = {CurrentFormatVersion}");
        text.AppendLine();
        text.AppendLine("[options]");
        text.AppendLine($"defaultDeadzone = {Number(profile.Options.DefaultDeadzone)}");
        text.AppendLine($"axisEpsilon = {Number(profile.Options.DefaultAxisEpsilon)}");

        foreach (Axis2Definition axis in profile.Axis2.Values.OrderBy(value => value.Id.Name, StringComparer.Ordinal))
        {
            text.AppendLine();
            text.AppendLine("[[axis2]]");
            text.AppendLine($"id = {Quote(axis.Id.Name)}");
            text.AppendLine($"x = {Quote(axis.X.Name)}");
            text.AppendLine($"y = {Quote(axis.Y.Name)}");
        }

        foreach (ActionMapDefinition map in profile.Maps.Values.OrderByDescending(value => value.Priority).ThenBy(value => value.Id.Name, StringComparer.Ordinal))
        {
            text.AppendLine();
            text.AppendLine("[[maps]]");
            text.AppendLine($"id = {Quote(map.Id.Name)}");
            text.AppendLine($"priority = {map.Priority}");
            text.AppendLine($"canConsume = {map.CanConsume.ToString().ToLowerInvariant()}");

            foreach (Binding binding in map.Bindings.OrderBy(value => value.Name, StringComparer.Ordinal))
            {
                text.AppendLine();
                text.AppendLine("[[maps.bindings]]");
                text.AppendLine($"name = {Quote(binding.Name)}");
                text.AppendLine($"trigger = {Quote(binding.Trigger.Type.ToString())}");
                text.AppendLine($"control = {Quote(ControlPath.Format(binding.Trigger.Control, binding.Trigger.Type))}");
                if (binding.Trigger.Type == TriggerType.Button)
                {
                    text.AppendLine($"edge = {Quote(binding.Trigger.ButtonEdge.ToString())}");
                }
                else if (binding.Trigger.Threshold != 0f)
                {
                    text.AppendLine($"threshold = {Number(binding.Trigger.Threshold)}");
                }

                if (binding.Trigger.Modifiers.Length > 0)
                {
                    text.AppendLine($"modifiers = [{string.Join(", ", binding.Trigger.Modifiers.Select(value => Quote(ControlPath.Format(value))))}]");
                }

                switch (binding.Output)
                {
                    case ActionOutput action:
                        text.AppendLine("output = \"Action\"");
                        text.AppendLine($"action = {Quote(action.Action.Name)}");
                        break;
                    case AxisOutput axis:
                        text.AppendLine("output = \"Axis\"");
                        text.AppendLine($"axis = {Quote(axis.Axis.Name)}");
                        text.AppendLine($"scale = {Number(axis.Scale)}");
                        break;
                }

                text.AppendLine($"consume = {Quote(binding.Consume.ToString())}");
                if (binding.Processors.Count > 0)
                {
                    text.AppendLine($"processors = [{string.Join(", ", binding.Processors.Select(value => Quote(FormatProcessor(value))))}]");
                }
            }
        }

        return text.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static InputProfile Load(string toml)
    {
        ArgumentNullException.ThrowIfNull(toml);
        var maps = new List<MapBuilder>();
        var axes2 = new List<Axis2Definition>();
        MapBuilder? map = null;
        BindingBuilder? binding = null;
        Axis2Builder? axis2 = null;
        string section = "root";
        int? formatVersion = null;
        float axisEpsilon = 0.0001f;
        float defaultDeadzone = 0.15f;

        foreach (string rawLine in toml.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line == "[options]")
            {
                section = "options";
                binding = null;
                axis2 = null;
                continue;
            }
            if (line == "[[axis2]]")
            {
                section = "axis2";
                axis2 = new Axis2Builder();
                continue;
            }
            if (line == "[[maps]]")
            {
                section = "map";
                map = new MapBuilder();
                maps.Add(map);
                binding = null;
                continue;
            }
            if (line == "[[maps.bindings]]")
            {
                if (map is null)
                {
                    throw new FormatException("A maps.bindings table must follow a maps table.");
                }
                section = "binding";
                binding = new BindingBuilder();
                map.Bindings.Add(binding);
                continue;
            }

            (string key, string value) = ParseAssignment(line);
            switch (section)
            {
                case "root" when key == "formatVersion":
                    formatVersion = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "options" when key == "axisEpsilon":
                    axisEpsilon = Float(value);
                    break;
                case "options" when key == "defaultDeadzone":
                    defaultDeadzone = Float(value);
                    break;
                case "axis2":
                    axis2!.Set(key, String(value));
                    if (axis2.Complete)
                    {
                        axes2.Add(axis2.Build());
                    }
                    break;
                case "map":
                    map!.Set(key, value);
                    break;
                case "binding":
                    binding!.Set(key, value);
                    break;
                default:
                    throw new FormatException($"Unsupported TOML field '{key}' in section '{section}'.");
            }
        }

        if (formatVersion != CurrentFormatVersion)
        {
            throw new NotSupportedException($"InputMan TOML formatVersion '{formatVersion?.ToString() ?? "missing"}' is unsupported; expected {CurrentFormatVersion}.");
        }

        InputProfile profile = Input.Profile(
            maps.Select(value => value.Build()),
            axes2,
            new InputOptions { DefaultDeadzone = defaultDeadzone, DefaultAxisEpsilon = axisEpsilon });
        return profile;
    }

    public static void SaveToFile(InputProfile profile, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, Save(profile));
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public static InputProfile LoadFromFile(string path) => Load(File.ReadAllText(path));

    private static string FormatProcessor(IProcessor processor) => processor switch
    {
        DeadzoneProcessor value => $"Deadzone:{Number(value.Deadzone)}",
        ScaleProcessor value => $"Scale:{Number(value.Scale)}",
        InvertProcessor => "Invert",
        ClampProcessor value => $"Clamp:{Number(value.Minimum)}:{Number(value.Maximum)}",
        _ => throw new NotSupportedException($"Processor '{processor.GetType().Name}' is not supported by TOML format version 1."),
    };

    private static IProcessor ParseProcessor(string value)
    {
        string[] parts = value.Split(':');
        return parts[0] switch
        {
            "Deadzone" when parts.Length == 2 => new DeadzoneProcessor(Float(parts[1])),
            "Scale" when parts.Length == 2 => new ScaleProcessor(Float(parts[1])),
            "Invert" when parts.Length == 1 => new InvertProcessor(),
            "Clamp" when parts.Length == 3 => new ClampProcessor(Float(parts[1]), Float(parts[2])),
            _ => throw new FormatException($"Unsupported processor '{value}'."),
        };
    }

    private static (string Key, string Value) ParseAssignment(string line)
    {
        int equals = line.IndexOf('=');
        if (equals <= 0)
        {
            throw new FormatException($"Expected key = value, got '{line}'.");
        }
        return (line[..equals].Trim(), line[(equals + 1)..].Trim());
    }

    private static string StripComment(string line)
    {
        bool quoted = false;
        for (int index = 0; index < line.Length; index++)
        {
            if (line[index] == '"') quoted = !quoted;
            if (line[index] == '#' && !quoted) return line[..index];
        }
        return line;
    }

    private static string String(string value)
    {
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
        {
            throw new FormatException($"Expected TOML string, got '{value}'.");
        }
        return value[1..^1].Replace("\\\"", "\"", StringComparison.Ordinal).Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    private static string Quote(string value) => $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    private static float Float(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed class MapBuilder
    {
        public string Id { get; private set; } = "";
        public int Priority { get; private set; }
        public bool CanConsume { get; private set; } = true;
        public List<BindingBuilder> Bindings { get; } = [];
        public void Set(string key, string value)
        {
            if (key == "id") Id = String(value);
            else if (key == "priority") Priority = int.Parse(value, CultureInfo.InvariantCulture);
            else if (key == "canConsume") CanConsume = bool.Parse(value);
            else throw new FormatException($"Unsupported map field '{key}'.");
        }
        public ActionMapDefinition Build() => Input.Map(new ActionMapId(Id), Priority, Bindings.Select(value => value.Build()), CanConsume);
    }

    private sealed class Axis2Builder
    {
        private string id = "";
        private string x = "";
        private string y = "";
        public bool Complete => id.Length > 0 && x.Length > 0 && y.Length > 0;
        public void Set(string key, string value)
        {
            if (key == "id") id = value;
            else if (key == "x") x = value;
            else if (key == "y") y = value;
            else throw new FormatException($"Unsupported axis2 field '{key}'.");
        }
        public Axis2Definition Build() => Input.Axis2(new Axis2Id(id), new AxisId(x), new AxisId(y));
    }

    private sealed class BindingBuilder
    {
        private string name = "";
        private TriggerType trigger = TriggerType.Button;
        private string control = "";
        private ButtonEdge edge = ButtonEdge.Down;
        private float threshold;
        private string output = "";
        private string outputId = "";
        private float scale = 1f;
        private ConsumeMode consume;
        private readonly List<ControlKey> modifiers = [];
        private readonly List<IProcessor> processors = [];

        public void Set(string key, string value)
        {
            if (key == "name") name = String(value);
            else if (key == "trigger") trigger = Enum.Parse<TriggerType>(String(value), true);
            else if (key == "control") control = String(value);
            else if (key == "edge") edge = Enum.Parse<ButtonEdge>(String(value), true);
            else if (key == "threshold") threshold = Float(value);
            else if (key == "output") output = String(value);
            else if (key == "action") outputId = String(value);
            else if (key == "axis") outputId = String(value);
            else if (key == "scale") scale = Float(value);
            else if (key == "consume") consume = Enum.Parse<ConsumeMode>(String(value), true);
            else if (key == "processors")
            {
                string inner = value.Trim()[1..^1];
                foreach (string item in inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    processors.Add(ParseProcessor(String(item)));
                }
            }
            else if (key == "modifiers")
            {
                string inner = value.Trim()[1..^1];
                foreach (string item in inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    modifiers.Add(ControlPath.Parse(String(item), TriggerType.Button));
                }
            }
            else throw new FormatException($"Unsupported binding field '{key}'.");
        }

        public Binding Build()
        {
            TriggerType resolvedTrigger = trigger;
            BindingOutput resolvedOutput = output switch
            {
                "Action" => new ActionOutput(new ActionId(outputId)),
                "Axis" => new AxisOutput(new AxisId(outputId), scale),
                _ => throw new FormatException($"Binding '{name}' has unsupported output '{output}'."),
            };
            return new Binding
            {
                Name = name,
                Trigger = new BindingTrigger
                {
                    Type = resolvedTrigger,
                    Control = ControlPath.Parse(control, resolvedTrigger),
                    ButtonEdge = edge,
                    Threshold = threshold,
                    Modifiers = [.. modifiers],
                },
                Output = resolvedOutput,
                Consume = consume,
                Processors = processors,
            };
        }
    }
}
