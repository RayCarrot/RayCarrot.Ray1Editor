﻿using BinarySerializer;
using RayCarrot.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RayCarrot.Ray1Editor
{
    public class EditorViewModel : AppViewBaseViewModel
    {
        #region Constructor

        public EditorViewModel(UserData_Game currentGame, GameManager currentGameManager, object currentGameSettings)
        {
            // Set properties
            CurrentGame = currentGame;
            CurrentGameManager = currentGameManager;
            CurrentGameSettings = currentGameSettings;
            Layers = new ObservableCollection<LayerEditorViewModel>();
            GameObjects = new ObservableCollection<GameObjectListItemViewModel>();
            ObjFields = new ObservableCollection<EditorFieldViewModel>();
            ShowObjFields = false;

            // Create commands
            LoadOtherMapCommand = new RelayCommand(LoadOtherMap);
            ResetPositionCommand = new RelayCommand(ResetPosition);
            SaveCommand = new RelayCommand(Save);
        }

        #endregion

        #region Commands

        public ICommand LoadOtherMapCommand { get; }
        public ICommand ResetPositionCommand { get; }
        public ICommand SaveCommand { get; }

        #endregion

        #region Private Fields

        private GameObjectListItemViewModel _selectedGameObjectItem;

        #endregion

        #region Public Properties

        // Game data
        public UserData_Game CurrentGame { get; }
        public GameManager CurrentGameManager { get; }
        public object CurrentGameSettings { get; }

        // Editor
        public EditorScene EditorScene { get; set; }
        public EditorMode Mode
        {
            get => EditorScene?.Mode ?? EditorMode.None;
            set => EditorScene.Mode = value;
        }
        public GameObject SelectedObject { get; set; }

        // Layers
        public ObservableCollection<LayerEditorViewModel> Layers { get; }

        // Object
        public ObservableCollection<GameObjectListItemViewModel> GameObjects { get; }

        public GameObjectListItemViewModel SelectedGameObjectItem
        {
            get => _selectedGameObjectItem;
            set
            {
                _selectedGameObjectItem = value;
                EditorScene.SelectedObject = value.Obj;
                EditorScene.GoToObject(value.Obj);
            }
        }

        public bool ShowObjFields { get; set; }
        public ObservableCollection<EditorFieldViewModel> ObjFields { get; }

        // TODO: Clean up
        public string DebugText { get; set; }
        public string SelectedObjectName { get; set; }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            // Load the editor
            LoadEditor();
        }

        public void LoadOtherMap()
        {
            App.ChangeView(AppViewModel.AppView.LoadMap, new LoadMapViewModel());
        }

        public void LoadEditor()
        {
            // Make sure any old editor instance gets unloaded
            UnloadEditor();

            // Recreate the fields
            RecreateObjFields();
            
            // Create a new editor scene instance for the current game
            EditorScene = new EditorScene(
                manager: CurrentGameManager,
                context: new Context(CurrentGame.Path),
                gameSettings: CurrentGameSettings);

            // Default to objects mode
            Mode = EditorMode.Objects;
        }

        public void OnEditorLoaded()
        {
            // Set up layers
            Layers.AddRange(EditorScene.GameData.Layers.Select(x => new LayerEditorViewModel(x)));

            // Create layer fields
            foreach (var layer in Layers)
            {
                layer.RecreateFields();
                layer.RefreshFields();
            }

            // Create object items
            GameObjects.AddRange(EditorScene.GameData.Objects.Select(x => new GameObjectListItemViewModel(x)));
        }

        public void UnloadEditor()
        {
            // Dispose the current instance (this will also unload the monogame resources)
            EditorScene?.Dispose();

            // Reset values
            EditorScene = null;
            SelectedObject = null;
            DebugText = null;
            SelectedObjectName = null;
            Layers.Clear();
            ShowObjFields = false;
            ObjFields.Clear();
        }

        public void UpdateSelectedObject(GameObject obj)
        {
            SelectedObject = obj;

            _selectedGameObjectItem = obj == null ? null : GameObjects.First(x => x.Obj == obj);
            OnPropertyChanged(nameof(SelectedGameObjectItem));

            SelectedObjectName = obj?.PrimaryName;
            RefreshObjFields();
        }

        public void RecreateObjFields()
        {
            // Clear previous fields
            ObjFields.Clear();

            // Add general fields
            // TODO: Update position when object moves
            ObjFields.Add(new EditorPointFieldViewModel(
                header: "Position", 
                info: null, 
                getValueAction: () => SelectedObject.Position, 
                setValueAction: x => SelectedObject.Position = x, 
                min: Int32.MinValue, 
                max: Int32.MaxValue));
            
            // Add game-specific fields
            ObjFields.AddRange(CurrentGameManager.GetEditorObjFields(() => SelectedObject));
        }

        public void RefreshObjFields()
        {
            if (SelectedObject == null)
            {
                ShowObjFields = false;
            }
            else
            {
                ShowObjFields = true;

                foreach (var field in ObjFields)
                    field.Refresh();
            }
        }

        public void ResetPosition()
        {
            EditorScene.ResetCamera();
        }

        public void Save()
        {
            // TODO: Try/catch
            EditorScene.Save();
            MessageBox.Show("Saved!"); // TODO: Have custom dialog window
        }

        public override void Dispose()
        {
            base.Dispose();

            UnloadEditor();
        }

        #endregion
    }
}