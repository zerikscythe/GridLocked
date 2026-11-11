using System;
using System.Collections.Generic;
using System.Drawing;

namespace GridLocked
{
    /// <summary>
    /// Represents a point light source (such as a wall lantern or torch).
    /// Supports a fully lit inner radius, a smooth falloff outer radius, and a 2D radial shadow map.
    /// </summary>
    public sealed class PointLight
    {
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>Fully lightens an area X units from the lantern (100% illumination).</summary>
        public double InnerRadius { get; set; } = 1.5;

        /// <summary>Distance at which light completely dims out to 0.</summary>
        public double OuterRadius { get; set; } = 5.5;

        /// <summary>Base light intensity multiplier.</summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>Color tint of the light (e.g. warm fire/lantern glow).</summary>
        public Color LightColor { get; set; } = Color.FromArgb(255, 235, 195);

        public bool Enabled { get; set; } = true;

        public const int ShadowRays = 2880; // 4x resolution: 0.125° per ray for ultra-crisp shadows
        public float[] ShadowMap { get; private set; } = new float[ShadowRays];

        public PointLight(double x, double y, double innerRadius = 1.5, double outerRadius = 5.5)
        {
            X = x;
            Y = y;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }

        /// <summary>
        /// Casts 360-degree DDA rays through the map grid to bake the distance to solid walls.
        /// </summary>
        public void BakeShadowMap(int[,] map)
        {
            if (map == null) return;
            int mapH = map.GetLength(0);
            int mapW = map.GetLength(1);

            double twoPi = Math.PI * 2.0;
            double stepAngle = twoPi / ShadowRays;

            for (int i = 0; i < ShadowRays; i++)
            {
                double angle = i * stepAngle;
                double dirX = Math.Cos(angle);
                double dirY = Math.Sin(angle);

                int mapX = (int)Math.Floor(X);
                int mapY = (int)Math.Floor(Y);

                double deltaDistX = (dirX == 0) ? 1e30 : Math.Abs(1.0 / dirX);
                double deltaDistY = (dirY == 0) ? 1e30 : Math.Abs(1.0 / dirY);

                int stepX, stepY;
                double sideDistX, sideDistY;

                if (dirX < 0) { stepX = -1; sideDistX = (X - mapX) * deltaDistX; }
                else { stepX = 1; sideDistX = (mapX + 1.0 - X) * deltaDistX; }

                if (dirY < 0) { stepY = -1; sideDistY = (Y - mapY) * deltaDistY; }
                else { stepY = 1; sideDistY = (mapY + 1.0 - Y) * deltaDistY; }

                double hitDist = OuterRadius;
                int maxSteps = 128;

                for (int s = 0; s < maxSteps; s++)
                {
                    bool sideVert;
                    if (sideDistX < sideDistY)
                    {
                        sideDistX += deltaDistX;
                        mapX += stepX;
                        sideVert = true;
                    }
                    else
                    {
                        sideDistY += deltaDistY;
                        mapY += stepY;
                        sideVert = false;
                    }

                    if (mapX < 0 || mapY < 0 || mapX >= mapW || mapY >= mapH)
                    {
                        hitDist = OuterRadius;
                        break;
                    }

                    if (map[mapY, mapX] != 0) // wall hit
                    {
                        double dist = sideVert
                            ? (mapX - X + (1 - stepX) * 0.5) / (dirX == 0 ? 1e-9 : dirX)
                            : (mapY - Y + (1 - stepY) * 0.5) / (dirY == 0 ? 1e-9 : dirY);

                        if (dist < 0.0001) dist = 0.0001;
                        hitDist = Math.Min(dist, OuterRadius);
                        break;
                    }

                    if (Math.Min(sideDistX, sideDistY) > OuterRadius)
                    {
                        hitDist = OuterRadius;
                        break;
                    }
                }

                ShadowMap[i] = (float)hitDist;
            }
        }
    }

    /// <summary>
    /// Manages dungeon light sources, ambient shading, overlapping light accumulation, and shadow casting.
    /// </summary>
    public sealed class LightingManager
    {
        public List<PointLight> Lights { get; } = new List<PointLight>();

        /// <summary>Global ambient brightness (0.0 allows unlit areas to reach true 0 light).</summary>
        public float AmbientLight { get; set; } = 0.0f;

        /// <summary>Maximum brightness for pure white in complete darkness (default 4% darkvision, 3 taps down from 10%).</summary>
        public float DarkMaxBrightness { get; set; } = 0.04f;

        /// <summary>Maximum view depth cap in tiles. Beyond this distance, darkness reaches 0% brightness (pitch black).</summary>
        public float MaxViewDepth { get; set; } = 14.5f;

        /// <summary>Minimum view depth in tiles where darkness starts attenuating. Closest to light/player (dist <= MinViewDepth), darkness has full DarkMaxBrightness.</summary>
        public float MinViewDepth { get; set; } = 2.5f;

        /// <summary>When true, colors invert in complete darkness (white becomes black, black becomes 10% white).</summary>
        public bool InvertColorsInDarkness { get; set; } = true;

        /// <summary>Master switch for lighting.</summary>
        public bool EnableLighting { get; set; } = true;

        /// <summary>Master switch for wall-cast shadows on ground and walls.</summary>
        public bool EnableShadows { get; set; } = true;

        /// <summary>Draw floor tile mortar lines for dungeon crawler depth.</summary>
        public bool ShowFloorTiles { get; set; } = true;

        // Pre-cached float color channels to avoid redundant division per-pixel
        private float _sr = 25f / 255f;
        private float _sg = 25f / 255f;
        private float _sb = 30f / 255f;

        private Color _shadowColor = Color.FromArgb(25, 25, 30);
        public Color ShadowColor
        {
            get => _shadowColor;
            set
            {
                _shadowColor = value;
                _sr = value.R / 255f;
                _sg = value.G / 255f;
                _sb = value.B / 255f;
            }
        }

        public Color LitFloorColor { get; set; } = Color.FromArgb(225, 220, 205);
        public Color LitWallColor { get; set; } = Color.FromArgb(175, 175, 175);
        public Color MortarColor { get; set; } = Color.FromArgb(35, 35, 35);
        public Color FloorTileLineColor { get; set; } = Color.FromArgb(45, 45, 50);

        public void ClearLights()
        {
            Lights.Clear();
        }

        public void AddLight(PointLight light)
        {
            if (light != null)
                Lights.Add(light);
        }

        /// <summary>
        /// Direct point calculation of light intensity and color tint.
        /// Used by FloorLightMap baking and direct queries.
        /// When normX/normY are supplied (for wall faces), lights located behind the wall face are culled.
        /// </summary>
        public float ComputeLightDirect(double wx, double wy, out float rTint, out float gTint, out float bTint, double normX = 0, double normY = 0)
        {
            rTint = 1.0f;
            gTint = 1.0f;
            bTint = 1.0f;

            if (!EnableLighting)
                return 1.0f;

            float totalIntensity = AmbientLight;
            float totalR = AmbientLight * (ShadowColor.R / 255f);
            float totalG = AmbientLight * (ShadowColor.G / 255f);
            float totalB = AmbientLight * (ShadowColor.B / 255f);

            double twoPi = Math.PI * 2.0;

            for (int i = 0; i < Lights.Count; i++)
            {
                var light = Lights[i];
                if (!light.Enabled) continue;

                // Backface normal culling for wall faces: light cannot illuminate a wall from behind the wall!
                if (normX != 0 || normY != 0)
                {
                    double toLightX = light.X - wx;
                    double toLightY = light.Y - wy;
                    double dot = normX * toLightX + normY * toLightY;
                    if (dot <= 0.001) // light is behind or tangent to the wall face
                        continue;
                }

                double dx = wx - light.X;
                double dy = wy - light.Y;
                double distSq = dx * dx + dy * dy;
                double outerSq = light.OuterRadius * light.OuterRadius;
                if (distSq >= outerSq) continue;

                double dist = Math.Sqrt(distSq);

                // --- 2D Wall Shadow Check ---
                float shadowCoeff = 1.0f;
                if (EnableShadows)
                {
                    double angle = Math.Atan2(dy, dx);
                    if (angle < 0) angle += twoPi;

                    int rayIdx = (int)Math.Round((angle / twoPi) * PointLight.ShadowRays) % PointLight.ShadowRays;
                    if (rayIdx < 0) rayIdx += PointLight.ShadowRays;

                    // 5-tap weighted soft penumbra filter
                    float dEps = (float)dist - 0.05f;
                    float unblockedW = 0f;
                    if (light.ShadowMap[(rayIdx - 2 + PointLight.ShadowRays) % PointLight.ShadowRays] >= dEps) unblockedW += 1f;
                    if (light.ShadowMap[(rayIdx - 1 + PointLight.ShadowRays) % PointLight.ShadowRays] >= dEps) unblockedW += 2f;
                    if (light.ShadowMap[rayIdx] >= dEps) unblockedW += 3f;
                    if (light.ShadowMap[(rayIdx + 1) % PointLight.ShadowRays] >= dEps) unblockedW += 2f;
                    if (light.ShadowMap[(rayIdx + 2) % PointLight.ShadowRays] >= dEps) unblockedW += 1f;

                    if (unblockedW <= 0f)
                        continue; // fully in shadow of a wall

                    shadowCoeff = unblockedW / 9.0f; // smooth multi-tap penumbra on edges
                }

                // --- Distance Falloff ---
                float distAtten;
                if (dist <= light.InnerRadius)
                {
                    distAtten = 1.0f;
                }
                else
                {
                    double t = (dist - light.InnerRadius) / (light.OuterRadius - light.InnerRadius);
                    distAtten = (float)(1.0 - t * t * (3.0 - 2.0 * t));
                    if (distAtten < 0f) distAtten = 0f;
                }

                float finalLight = distAtten * shadowCoeff * light.Intensity;
                totalIntensity += finalLight;

                totalR += finalLight * (light.LightColor.R / 255f);
                totalG += finalLight * (light.LightColor.G / 255f);
                totalB += finalLight * (light.LightColor.B / 255f);
            }

            if (totalIntensity > 1.0f) totalIntensity = 1.0f;
            rTint = Math.Min(1.0f, totalR);
            gTint = Math.Min(1.0f, totalG);
            bTint = Math.Min(1.0f, totalB);

            return totalIntensity;
        }

        private FloorLightMap _lightMap = new FloorLightMap();

        public void RebakeAllShadows(int[,] map)
        {
            for (int i = 0; i < Lights.Count; i++)
            {
                Lights[i].BakeShadowMap(map);
            }
            if (map != null)
            {
                _lightMap.Bake(this, map);
            }
        }

        /// <summary>
        /// Fast real-time light sampling via pre-baked 2D lightmap.
        /// </summary>
        public float SampleLight(double wx, double wy, out float rTint, out float gTint, out float bTint)
        {
            if (!EnableLighting)
            {
                rTint = 1.0f; gTint = 1.0f; bTint = 1.0f;
                return 1.0f;
            }

            return _lightMap.Sample(wx, wy, out rTint, out gTint, out bTint);
        }

        /// <summary>
        /// Fast color shade with pre-calculated light intensity and tints.
        /// Supports inverted negative rendering in complete darkness with DarkMaxBrightness (e.g. 10%)
        /// and a view depth cap so unlit darkness gets perpetually darker out to MaxViewDepth (0% brightness).
        /// </summary>
        public int ShadeArgb(Color baseColor, float light, float rTint, float gTint, float bTint, double distance = 0)
        {
            if (!EnableLighting)
                return baseColor.ToArgb();

            // 1. Calculate depth attenuation factor based on view depth cap
            float depthFactor = 1.0f;
            if (MaxViewDepth > MinViewDepth)
            {
                if (distance <= MinViewDepth)
                    depthFactor = 1.0f;
                else if (distance >= MaxViewDepth)
                    depthFactor = 0.0f;
                else
                    depthFactor = (float)((MaxViewDepth - distance) / (MaxViewDepth - MinViewDepth));
            }
            else if (distance >= MaxViewDepth)
            {
                depthFactor = 0.0f;
            }

            // 2. Full light color (fades at extreme view depth cap if illuminated from far away)
            float litFade = Math.Min(1.0f, depthFactor * 1.5f);
            float litR = (baseColor.R / 255f) * rTint * litFade;
            float litG = (baseColor.G / 255f) * gTint * litFade;
            float litB = (baseColor.B / 255f) * bTint * litFade;

            // 3. Dark color in absence of light:
            // Closest to the light / player (distance <= MinViewDepth), brightness reaches DarkMaxBrightness (e.g. 10%).
            // As view depth increases towards MaxViewDepth, brightness scales perpetually down to 0% at the very far end.
            float effectiveDarkMax = DarkMaxBrightness * depthFactor;

            float darkR, darkG, darkB;
            if (InvertColorsInDarkness)
            {
                // Invert base color: (255 - baseColor)
                // Black mortar (0) inverts to White (255) * effectiveDarkMax (10% down to 0%)
                // White brick (255) inverts to Black (0)
                darkR = ((255 - baseColor.R) / 255f) * effectiveDarkMax;
                darkG = ((255 - baseColor.G) / 255f) * effectiveDarkMax;
                darkB = ((255 - baseColor.B) / 255f) * effectiveDarkMax;
            }
            else
            {
                darkR = (baseColor.R / 255f) * effectiveDarkMax;
                darkG = (baseColor.G / 255f) * effectiveDarkMax;
                darkB = (baseColor.B / 255f) * effectiveDarkMax;
            }

            // 4. Smooth transition from darkness (light = 0) to full light (light = 1)
            float inv = 1.0f - light;
            float r = darkR * inv + litR * light;
            float g = darkG * inv + litG * light;
            float b = darkB * inv + litB * light;

            int ir = (int)(r * 255f);
            int ig = (int)(g * 255f);
            int ib = (int)(b * 255f);
            if (ir > 255) ir = 255; else if (ir < 0) ir = 0;
            if (ig > 255) ig = 255; else if (ig < 0) ig = 0;
            if (ib > 255) ib = 255; else if (ib < 0) ib = 0;

            return (255 << 24) | (ir << 16) | (ig << 8) | ib;
        }
    }

    /// <summary>
    /// High-performance 2D floor lightmap grid for instantaneous O(1) floor pixel sampling.
    /// Eliminates per-pixel trigonometric DDA queries during rendering.
    /// </summary>
    public sealed class FloorLightMap
    {
        public const int Subdivisions = 64; // High-resolution spatial grid: 64x64 samples per tile
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float[] Intensity { get; private set; }
        public float[] RTint { get; private set; }
        public float[] GTint { get; private set; }
        public float[] BTint { get; private set; }

        public void Bake(LightingManager lm, int[,] map)
        {
            if (map == null) return;
            int mapH = map.GetLength(0);
            int mapW = map.GetLength(1);
            Width = mapW * Subdivisions;
            Height = mapH * Subdivisions;
            int total = Width * Height;

            if (Intensity == null || Intensity.Length != total)
            {
                Intensity = new float[total];
                RTint = new float[total];
                GTint = new float[total];
                BTint = new float[total];
            }

            double step = 1.0 / Subdivisions;
            int idx = 0;
            for (int y = 0; y < Height; y++)
            {
                double wy = (y + 0.5) * step;
                for (int x = 0; x < Width; x++)
                {
                    double wx = (x + 0.5) * step;
                    float light = lm.ComputeLightDirect(wx, wy, out float r, out float g, out float b);
                    Intensity[idx] = light;
                    RTint[idx] = r;
                    GTint[idx] = g;
                    BTint[idx] = b;
                    idx++;
                }
            }
        }

        public float Sample(double wx, double wy, out float r, out float g, out float b)
        {
            if (Intensity == null || Width == 0 || Height == 0)
            {
                r = 1f; g = 1f; b = 1f;
                return 1f;
            }

            // Bilinear interpolation between neighboring lightmap samples for silky-smooth shadows
            double gx = wx * Subdivisions - 0.5;
            double gy = wy * Subdivisions - 0.5;

            int x0 = (int)gx;
            int y0 = (int)gy;
            if (gx < 0 && gx != x0) x0--;
            if (gy < 0 && gy != y0) y0--;
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float fx = (float)(gx - x0);
            float fy = (float)(gy - y0);

            if (x0 < 0) x0 = 0; else if (x0 >= Width) x0 = Width - 1;
            if (x1 < 0) x1 = 0; else if (x1 >= Width) x1 = Width - 1;
            if (y0 < 0) y0 = 0; else if (y0 >= Height) y0 = Height - 1;
            if (y1 < 0) y1 = 0; else if (y1 >= Height) y1 = Height - 1;

            int row0 = y0 * Width;
            int row1 = y1 * Width;

            int idx00 = row0 + x0;
            int idx10 = row0 + x1;
            int idx01 = row1 + x0;
            int idx11 = row1 + x1;

            float w00 = (1f - fx) * (1f - fy);
            float w10 = fx * (1f - fy);
            float w01 = (1f - fx) * fy;
            float w11 = fx * fy;

            float light = Intensity[idx00] * w00 + Intensity[idx10] * w10 + Intensity[idx01] * w01 + Intensity[idx11] * w11;
            r = RTint[idx00] * w00 + RTint[idx10] * w10 + RTint[idx01] * w01 + RTint[idx11] * w11;
            g = GTint[idx00] * w00 + GTint[idx10] * w10 + GTint[idx01] * w01 + GTint[idx11] * w11;
            b = BTint[idx00] * w00 + BTint[idx10] * w10 + BTint[idx01] * w01 + BTint[idx11] * w11;

            return light;
        }
    }
}
