﻿using System.Collections.Generic;
using BinarySerializer;

namespace RayCarrot.Ray1Editor
{
    /// <summary>
    /// Game data to be stored for the editor and used by the game manager
    /// </summary>
    public abstract class GameData
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="context">The serializer context</param>
        protected GameData(Context context)
        {
            Context = context;
            Objects = new List<GameObject>();
            Layers = new List<Layer>();
        }

        /// <summary>
        /// The serializer context
        /// </summary>
        public Context Context { get; }

        /// <summary>
        /// The objects
        /// </summary>
        public List<GameObject> Objects { get; }

        /// <summary>
        /// The layers. Can be tile maps, backgrounds etc.
        /// </summary>
        public List<Layer> Layers { get; }

        /// <summary>
        /// The loaded palettes used by the game data
        /// </summary>
        public abstract IEnumerable<Palette> Palettes { get; }

        /// <summary>
        /// Loads the editor elements stored in the data
        /// </summary>
        /// <param name="e">The editor scene to load to</param>
        public void LoadElements(EditorScene e)
        {
            // Load objects
            foreach (var obj in Objects)
                obj.LoadElement(e);

            // Load layers
            foreach (var layer in Layers)
                layer.LoadElement(e);
        }
    }
}