﻿using BinarySerializer;
using BinarySerializer.Ray1;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using BinarySerializer.Image;
using Microsoft.Xna.Framework.Graphics;

namespace RayCarrot.Ray1Editor
{
    /// <summary>
    /// Game manager for Rayman 1 on PC
    /// </summary>
    public class GameManager_R1_PC : GameManager_R1
    {
        // TODO: Move some load code out from here and into R1 base class so it can be reused

        #region Paths

        public string Path_VigFile => $"VIGNET.DAT";
        public string Path_DataDir => $"PCMAP/";
        public string Path_FixFile => Path_DataDir + $"ALLFIX.DAT";
        public string Path_WorldFile(World world) => Path_DataDir + $"RAY{(int)world}.WLD";
        public string Path_LevelFile(World world, int level) => Path_DataDir + $"{world}/" + $"RAY{level}.LEV";

        #endregion

        #region Manager

        public override IEnumerable<LoadGameLevelViewModel> GetLevels()
        {
            // TODO: Don't hard-code the PC version - set based on game mode
            return GetLevels(Ray1EngineVersion.PC, Ray1PCVersion.PC_1_21);
        }

        public override GameData Load(Context context, object settings, TextureManager textureManager)
        {
            // Get settings
            var ray1Settings = (Ray1Settings)settings;
            var world = ray1Settings.World;
            var level = ray1Settings.Level;

            // Add the settings
            context.AddSettings(ray1Settings);

            // Add files
            context.AddFile(new LinearSerializedFile(context, Path_VigFile));
            context.AddFile(new LinearSerializedFile(context, Path_FixFile));
            context.AddFile(new LinearSerializedFile(context, Path_WorldFile(world)));
            context.AddFile(new LinearSerializedFile(context, Path_LevelFile(world, level)));

            // Create the data
            var data = new GameData_R1(context);

            // Read the files
            var fix = FileFactory.Read<PC_AllfixFile>(Path_FixFile, context);
            var wld = FileFactory.Read<PC_WorldFile>(Path_WorldFile(world), context);
            var lev = FileFactory.Read<PC_LevFile>(Path_LevelFile(world, level), context);

            // Load palettes
            LoadPalettes(data, lev);

            // Load fond (background)
            LoadFond(data, wld, lev, textureManager);

            // Load map
            LoadMap(data, lev, textureManager);

            // Load DES (sprites & animations)
            LoadDES(data, fix, wld, textureManager);

            // Load ETA (states)
            LoadETA(data, fix, wld);

            // Load objects
            LoadObjects(data, lev);

            return data;
        }

        public override void Save(Context context, GameData gameData)
        {
            // Get settings
            var ray1Settings = context.GetSettings<Ray1Settings>();
            var world = ray1Settings.World;
            var level = ray1Settings.Level;

            // Get the level data
            var lvlData = context.GetMainFileObject<PC_LevFile>(Path_LevelFile(world, level));

            // TODO: Update tiles
            // TODO: Update objects
            // TODO: Update links

            // Save the file
            FileFactory.Write<PC_LevFile>(Path_LevelFile(world, level), context);
        }

        public override IEnumerable<EditorFieldViewModel> GetEditorObjFields(Func<GameObject> getSelectedObj)
        {
            // Helper methods
            GameObject_R1 getObj() => (GameObject_R1)getSelectedObj();
            ObjData getObjData() => getObj().ObjData;

            // TODO: Position (general field?)

            // TODO: Do not include EDU/KIT types for R1
            var dropDownItems_type = Enum.GetValues(typeof(ObjType)).Cast<ObjType>().Select(x =>
                new EditorDropDownFieldViewModel.DropDownItem<ObjType>(x.ToString(), x)).ToArray();
            var dropDownItems_state = new Dictionary<ETA, EditorDropDownFieldViewModel.DropDownItem<DropDownFieldData_State>[]>();

            // TODO: SpriteIndex, AnimIndex, ImgBufferIndex
            // TODO: ETAIndex
            // TODO: Commands, labels

            yield return new EditorDropDownFieldViewModel(
                header: "Type",
                info: null,
                getValueAction: () => dropDownItems_type.FindItemIndex(x => x.Data == getObjData().Type),
                setValueAction: x => getObjData().Type = dropDownItems_type[x].Data,
                getItemsAction: () => dropDownItems_type);

            // TODO: BX, BY, HY

            yield return new EditorDropDownFieldViewModel(
                header: "State",
                info: "The object state (ETA), grouped by the primary state (etat) and sub-state (sub-etat). The state primarily determines which animation to play, but also other factors such as how the object should behave (based on the type).",
                getValueAction: () => dropDownItems_state[getObjData().ETA].FindItemIndex(x => x.Data.Etat == getObjData().Etat && x.Data.SubEtat == getObjData().SubEtat),
                setValueAction: x =>
                {
                    getObjData().Etat = dropDownItems_state[getObjData().ETA][x].Data.Etat;
                    getObjData().SubEtat = dropDownItems_state[getObjData().ETA][x].Data.SubEtat;
                },
                getItemsAction: () =>
                {
                    var eta = getObjData().ETA;

                    if (!dropDownItems_state.ContainsKey(eta))
                        dropDownItems_state[eta] = eta.States.Select((etat, etatIndex) => etat.Select((subEtat, subEtatIndex) =>
                            new EditorDropDownFieldViewModel.DropDownItem<DropDownFieldData_State>($"State {etatIndex}-{subEtatIndex} (Animation {subEtat.AnimationIndex})", new DropDownFieldData_State((byte)etatIndex, (byte)subEtatIndex)))).SelectMany(x => x).ToArray();

                    return dropDownItems_state[eta];
                });

            // TODO: FollowSprite

            // TODO: Increase max for EDU/KIT
            yield return new EditorIntFieldViewModel(
                header: "HitPoints",
                info: "This value usually determines how many hits it takes to defeat the enemy. For non-enemy objects this can have other usages, such as determining the color or changing other specific attributes.",
                getValueAction: () => getObjData().ActualHitPoints,
                setValueAction: x => getObjData().ActualHitPoints = (uint)x,
                max: 255);

            // TODO: DisplayPrio?

            yield return new EditorIntFieldViewModel(
                header: "HitSprite",
                info: null,
                getValueAction: () => getObjData().HitSprite,
                setValueAction: x => getObjData().HitSprite = (byte)x,
                max: 255);

            // TODO: FollowEnabled
        }

        #endregion

        #region Helpers

        public void LoadPalettes(GameData_R1 data, PC_LevFile lev)
        {
            // TODO: Allow multiple palettes, choose which to view in editor
            data.Palette = new Palette(lev.MapData.ColorPalettes[0]);
            data.Palette.SetFirstToTransparent();
        }

        public void LoadDES(GameData_R1 data, PC_AllfixFile fix, PC_WorldFile wld, TextureManager textureManager)
        {
            data.PC_DES = new PC_DES[]
            {
                null
            }.Concat(fix.DesItems).Concat(wld.DesItems).ToArray();

            data.PC_LoadedAnimations = new Animation[data.PC_DES.Length][];

            for (int i = 1; i < data.PC_DES.Length; i++)
            {
                var des = data.PC_DES[i];

                var processedImageData = des.RequiresBackgroundClearing ? PC_DES.ProcessImageData(des.ImageData) : des.ImageData;

                data.Sprites[des.Sprites] = new TextureSheet(textureManager.GraphicsDevice, des.Sprites.Select(x => x.IsDummySprite() ? (Point?)null : new Point(x.Width, x.Height)).ToArray()).AddToManager(textureManager);

                var spriteSheet = data.Sprites[des.Sprites];

                for (int j = 0; j < des.SpritesCount; j++)
                {
                    var spriteSheetEntry = spriteSheet.Entries[j];

                    if (spriteSheetEntry == null)
                        continue;

                    var sprite = des.Sprites[j];

                    spriteSheet.InitEntry(spriteSheetEntry, data.Palette, processedImageData.Skip((int)sprite.ImageBufferOffset).Take(sprite.Width * sprite.Height), sprite.Width * sprite.Height);
                }

                var loadedAnim = des.Animations.Select(x => new Animation
                {
                    LayersPerFrameSerialized = x.LayersPerFrame,
                    FrameCountSerialized = x.FrameCount,
                    Layers = x.Layers,
                    Frames = x.Frames
                }).ToArray();
                data.PC_LoadedAnimations[i] = loadedAnim;
                data.Animations[loadedAnim] = loadedAnim.Select(x => ToCommonAnimation(x)).ToArray();
            }
        }

        public void LoadETA(GameData_R1 data, PC_AllfixFile fix, PC_WorldFile wld)
        {
            data.ETA.AddRange(fix.Eta.Select(x => new ETA()
            {
                States = x.States
            }));
            data.ETA.AddRange(wld.Eta.Select(x => new ETA()
            {
                States = x.States
            }));
        }

        public void LoadObjects(GameData_R1 data, PC_LevFile lev)
        {
            data.Objects.AddRange(lev.ObjData.Objects.Select(x => new GameObject_R1(x)));

            foreach (var obj in data.Objects.Cast<GameObject_R1>())
            {
                obj.ObjData.Animations = data.PC_LoadedAnimations[(int)obj.ObjData.PC_AnimationsIndex];
                obj.ObjData.ImageBuffer = data.PC_DES[obj.ObjData.PC_SpritesIndex].ImageData;
                obj.ObjData.Sprites = data.PC_DES[obj.ObjData.PC_SpritesIndex].Sprites;
                obj.ObjData.ETA = data.ETA[(int)obj.ObjData.PC_ETAIndex];
            }

            InitLinkGroups(data.Objects, lev.ObjData.ObjLinkingTable);
        }

        public void LoadMap(GameData_R1 data, PC_LevFile lev, TextureManager textureManager)
        {
            var map = lev.MapData;

            var tileSize = new Point(Ray1Settings.CellSize, Ray1Settings.CellSize);

            var tileSheet = new TextureSheet(textureManager.GraphicsDevice, Enumerable.Repeat((Point?)tileSize, lev.TileTextureData.TexturesOffsetTable.Length).ToArray()).AddToManager(textureManager);

            var tileSet = new TileSet(tileSheet, tileSize);

            for (int i = 0; i < tileSet.TileSheet.Entries.Length; i++)
            {
                var tex = lev.TileTextureData.NonTransparentTextures.
                    Concat(lev.TileTextureData.TransparentTextures).
                    First(x => x.Offset == lev.TileTextureData.TexturesOffsetTable[i]);

                tileSet.TileSheet.InitEntry(tileSet.TileSheet.Entries[i], data.Palette, tex.ImgData.Select(x => (byte)(255 - x)), tex.ImgData.Length);
            }

            data.Layers.Add(new TileMapLayer(map.Tiles, Point.Zero, new Point(map.Width, map.Height), tileSet));
        }

        public void LoadFond(GameData_R1 data, PC_WorldFile wld, PC_LevFile lev, TextureManager textureManager)
        {
            var normalFnd = LoadFond(data, wld, lev, textureManager, false);
            var scrollDiffFnd = LoadFond(data, wld, lev, textureManager, true);

            data.Layers.Add(new BackgroundLayer(normalFnd, Point.Zero));

            if (scrollDiffFnd != null)
                data.Layers.Add(new BackgroundLayer(scrollDiffFnd, Point.Zero, $"Parallax Background")
                {
                    IsVisible = false
                });
        }

        public Texture2D LoadFond(GameData_R1 data, PC_WorldFile wld, PC_LevFile lev, TextureManager textureManager, bool parallax)
        {
            // Return null if the parallax bg is the same as the normal one
            if (parallax && lev.ScrollDiffFNDIndex == lev.FNDIndex)
                return null;

            var pcx = LoadArchiveFile<PCX>(data.Context, Path_VigFile, wld.Plan0NumPcx[parallax ? lev.ScrollDiffFNDIndex : lev.FNDIndex]);

            var tex = new Texture2D(textureManager.GraphicsDevice, pcx.ImageWidth, pcx.ImageHeight).AddToManager(textureManager);

            for (int y = 0; y < pcx.ImageHeight; y++)
                tex.SetData(0, new Rectangle(0, y, pcx.ImageWidth, 1), pcx.ScanLines[y].Select(x => data.Palette.Colors[x]).ToArray(), 0, pcx.ImageWidth);

            return tex;
        }

        public T LoadArchiveFile<T>(Context context, string archivePath, int fileIndex)
            where T : BinarySerializable, new()
        {
            if (context.MemoryMap.Files.All(x => x.FilePath != archivePath))
                return null;

            return FileFactory.Read<PC_FileArchive>(archivePath, context).ReadFile<T>(context, fileIndex);
        }

        public int InitLinkGroups(IList<GameObject> objects, ushort[] linkTable)
        {
            int currentId = 1;

            for (int i = 0; i < objects.Count; i++)
            {
                if (i >= linkTable.Length)
                    break;

                // No link
                if (linkTable[i] == i)
                {
                    objects[i].LinkGroup = 0;
                }
                else
                {
                    // Ignore already assigned ones
                    if (objects[i].LinkGroup != 0)
                        continue;

                    // Link found, loop through everyone on the link chain
                    int nextEvent = linkTable[i];
                    objects[i].LinkGroup = currentId;
                    int prevEvent = i;
                    while (nextEvent != i && nextEvent != prevEvent)
                    {
                        prevEvent = nextEvent;
                        objects[nextEvent].LinkGroup = currentId;
                        nextEvent = linkTable[nextEvent];
                    }
                    currentId++;
                }
            }

            return currentId;
        }

        #endregion

        #region Field Records

        protected record DropDownFieldData_State(byte Etat, byte SubEtat);

        #endregion
    }
}