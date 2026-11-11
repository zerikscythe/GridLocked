using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GridLocked
{
    /// <summary>
    /// Specifies how damage value is calculated against a target's health pool.
    /// </summary>
    public enum DamageValueType
    {
        /// <summary>Absolute point damage (e.g., 25 damage points).</summary>
        Flat,
        /// <summary>Percentage based on the target's Maximum Health (e.g., 15% Max HP).</summary>
        PercentMaxHp,
        /// <summary>Percentage based on the target's Current Health (e.g., 25% Current HP).</summary>
        PercentCurrentHp
    }

    /// <summary>
    /// Source-typed damage and status effect payload.
    /// Used by floor hazards, traps, creature abilities, weapon attacks, and environmental hazards.
    /// The damage type is a string that resolves against the project's user-defined Damage Types list.
    /// </summary>
    public sealed class Damage
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Normal";
        public DamageValueType ValueType { get; set; } = DamageValueType.Flat;
        public double Amount { get; set; } = 10;
        public string StatusEffect { get; set; } = string.Empty;
        public float ProcChance { get; set; } = 1.0f;
        public int Duration { get; set; } = 0;

        public Damage() { }

        public Damage(string name, string type, double amount, DamageValueType valueType = DamageValueType.Flat, string statusEffect = "")
        {
            Name = name;
            Type = string.IsNullOrWhiteSpace(type) ? "Normal" : type;
            Amount = amount;
            ValueType = valueType;
            StatusEffect = statusEffect ?? string.Empty;
            Id = name.ToLowerInvariant().Replace(' ', '_');
        }

        public Damage Clone()
        {
            return new Damage
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                ValueType = this.ValueType,
                Amount = this.Amount,
                StatusEffect = this.StatusEffect,
                ProcChance = this.ProcChance,
                Duration = this.Duration
            };
        }

        /// <summary>
        /// Computes the discrete damage amount inflicted on a target given their health stats.
        /// </summary>
        public int Calculate(int currentHp, int maxHp)
        {
            switch (ValueType)
            {
                case DamageValueType.PercentMaxHp:
                    int maxPool = Math.Max(1, maxHp);
                    return (int)Math.Max(1, Math.Round(maxPool * (Amount / 100.0)));

                case DamageValueType.PercentCurrentHp:
                    int curPool = Math.Max(1, currentHp);
                    return (int)Math.Max(1, Math.Round(curPool * (Amount / 100.0)));

                case DamageValueType.Flat:
                default:
                    return (int)Math.Max(0, Math.Round(Amount));
            }
        }

        /// <summary>
        /// Human-readable summary badge, e.g. "20 Physical (Flat) + Bleed" or "15% Max HP Fire + Burn".
        /// </summary>
        public string Summary
        {
            get
            {
                string scaleStr;
                switch (ValueType)
                {
                    case DamageValueType.PercentMaxHp:
                        scaleStr = "% MaxHP";
                        break;
                    case DamageValueType.PercentCurrentHp:
                        scaleStr = "% CurHP";
                        break;
                    default:
                        scaleStr = "";
                        break;
                }

                string effStr = string.IsNullOrWhiteSpace(StatusEffect) ? "" : $" + {StatusEffect}";
                return $"{Amount}{scaleStr} {Type}{effStr}";
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Name)) return Summary;
            return $"{Name} [{Summary}]";
        }

        /// <summary>
        /// Default list of damage types provided when starting a new project.
        /// </summary>
        public static List<string> CreateDefaultDamageTypes()
        {
            return new List<string>
            {
                "Normal",
                "Physical",
                "Magic",
                "Fire",
                "Frost",
                "Lightning",
                "Toxic",
                "Laser",
                "Plasma",
                "Arcane",
                "Mental",
                "Sonic",
                "EMP",
                "Void"
            };
        }

        /// <summary>
        /// Default hazard presets for starters.
        /// </summary>
        public static List<Damage> CreateDefaultPresets()
        {
            return new List<Damage>
            {
                new Damage("Spike Impale", "Physical", 20, DamageValueType.Flat, "Bleed") { Id = "spikes" },
                new Damage("Lava Scald", "Fire", 15, DamageValueType.PercentMaxHp, "Burn") { Id = "lava" },
                new Damage("Acid Burn", "Toxic", 15, DamageValueType.Flat, "Poison") { Id = "acid" },
                new Damage("Poison Dart", "Physical", 10, DamageValueType.Flat, "Poison") { Id = "poison_dart" },
                new Damage("Arcane Shock", "Magic", 25, DamageValueType.Flat, "Stun") { Id = "arcane_shock" }
            };
        }

        /// <summary>
        /// Parses a Damage definition from a JsonNode. Supports both rich JSON objects and legacy flat numbers.
        /// </summary>
        public static Damage FromJson(JsonNode node)
        {
            if (node == null) return null;

            // Legacy plain number support: e.g. "hazardDamage": 25
            if (node.ObjectValue == null)
            {
                double num = node.NumberValue;
                if (num <= 0 && double.TryParse(node.StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                    num = parsed;

                if (num <= 0) return null;
                return new Damage("Legacy Hazard", "Normal", num, DamageValueType.Flat);
            }

            var d = new Damage
            {
                Id = node.GetString("id", ""),
                Name = node.GetString("name", "Hazard"),
                Type = node.GetString("type", "Normal"),
                Amount = node.GetDouble("amount", 10.0),
                StatusEffect = node.GetString("statusEffect", ""),
                ProcChance = (float)node.GetDouble("procChance", 1.0),
                Duration = node.GetInt("duration", 0)
            };

            string vType = node.GetString("valueType", "Flat");
            if (Enum.TryParse<DamageValueType>(vType, true, out var vt))
                d.ValueType = vt;
            else if (vType.IndexOf("max", StringComparison.OrdinalIgnoreCase) >= 0)
                d.ValueType = DamageValueType.PercentMaxHp;
            else if (vType.IndexOf("cur", StringComparison.OrdinalIgnoreCase) >= 0)
                d.ValueType = DamageValueType.PercentCurrentHp;

            return d;
        }

        /// <summary>
        /// Serializes this Damage object into JSON string format.
        /// </summary>
        public string ToJsonString(int indent = 0)
        {
            string pad = new string(' ', indent);
            var sb = new StringBuilder();
            sb.AppendLine(pad + "{");
            if (!string.IsNullOrEmpty(Id))
                sb.AppendLine(pad + $"  \"id\": \"{EscapeJson(Id)}\",");
            sb.AppendLine(pad + $"  \"name\": \"{EscapeJson(Name)}\",");
            sb.AppendLine(pad + $"  \"type\": \"{EscapeJson(Type)}\",");
            sb.AppendLine(pad + $"  \"amount\": {Amount.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine(pad + $"  \"valueType\": \"{ValueType}\",");
            if (!string.IsNullOrEmpty(StatusEffect))
                sb.AppendLine(pad + $"  \"statusEffect\": \"{EscapeJson(StatusEffect)}\",");
            sb.AppendLine(pad + $"  \"procChance\": {ProcChance.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine(pad + $"  \"duration\": {Duration}");
            sb.Append(pad + "}");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        }
    }
}
