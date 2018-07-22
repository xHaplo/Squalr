﻿namespace Squalr.Engine.Projects
{
    using SharpDX.DirectInput;
    using Squalr.Engine.Input.HotKeys;
    using Squalr.Engine.Logging;
    using Squalr.Engine.Projects.Properties;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;

    /// <summary>
    /// A base class for all project items that can be added to the project explorer.
    /// </summary>
    [KnownType(typeof(ProjectItem))]
    [KnownType(typeof(ScriptItem))]
    [KnownType(typeof(AddressItem))]
    [KnownType(typeof(InstructionItem))]
    [KnownType(typeof(PointerItem))]
    [KnownType(typeof(DotNetItem))]
    [KnownType(typeof(JavaItem))]
    [DataContract]
    public class ProjectItem : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// The name of this project item.
        /// </summary>
        [Browsable(false)]
        protected String name;

        /// <summary>
        /// The description of this project item.
        /// </summary>
        [Browsable(false)]
        protected String description;

        /// <summary>
        /// The unique identifier of this project item.
        /// </summary>
        [Browsable(false)]
        protected Guid guid;

        /// <summary>
        /// The hotkey associated with this project item.
        /// </summary>
        [Browsable(false)]
        protected Hotkey hotkey;

        /// <summary>
        /// A value indicating whether this project item has been activated.
        /// </summary>
        [Browsable(false)]
        protected Boolean isActivated;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectItem" /> class.
        /// </summary>
        internal ProjectItem(String path) : this(path, String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectItem" /> class.
        /// </summary>
        /// <param name="description">The description of the project item.</param>
        internal ProjectItem(String path, String description)
        {
            // Bypass setters/getters to avoid triggering any view updates in constructor
            this.name = description ?? String.Empty;
            this.isActivated = false;
            this.guid = Guid.NewGuid();
            this.ActivationLock = new Object();
        }

        public static ProjectItem FromFile(String filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new Exception("File does not exist: " + filePath);
                }

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    Type type = null;

                    switch ((new FileInfo(filePath).Extension).ToLower())
                    {
                        case ".mod":
                            type = typeof(ScriptItem);
                            break;
                        case ".ptr":
                            type = typeof(PointerItem);
                            break;
                        case ".ins":
                            type = typeof(InstructionItem);
                            break;
                        case ".clr":
                            type = typeof(DotNetItem);
                            break;
                        case ".jvm":
                            type = typeof(JavaItem);
                            break;
                        default:
                            return null;
                    }

                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);

                    ProjectItem projectItem = serializer.ReadObject(fileStream) as ProjectItem;

                    if (projectItem != null)
                    {
                        projectItem.DirectoryPath = Path.GetDirectoryName(filePath);
                    }

                    return projectItem;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Error loading file", ex);
                throw ex;
            }
        }

        protected static void Save(ProjectItem projectItem, String directoryPath = null)
        {
            String filePath = String.Empty;
            String name = projectItem.Name;

            if (String.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = ProjectSettings.Default.ProjectRoot;
            }

            if (String.IsNullOrWhiteSpace(projectItem.Name))
            {
                name = projectItem.Guid.ToString();
            }

            filePath = Path.Combine(directoryPath, name);

            switch (projectItem?.GetType())
            {
                case Type type when projectItem is PointerItem:
                    filePath += ".ptr";
                    break;
                case Type type when projectItem is ScriptItem:
                    filePath += ".mod";
                    break;
                case Type type when projectItem is InstructionItem:
                    filePath += ".ins";
                    break;
                case Type type when projectItem is JavaItem:
                    filePath += ".jvm";
                    break;
                case Type type when projectItem is DotNetItem:
                    filePath += ".clr";
                    break;
            }

            try
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(projectItem.GetType());
                    serializer.WriteObject(fileStream, projectItem);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Error saving file", ex);
                throw ex;
            }
        }

        /// <summary>
        /// Occurs after a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public String DirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the description for this object.
        /// </summary>
        [DataMember]
        public virtual String Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (this.name == value)
                {
                    return;
                }

                this.name = value;
                this.RaisePropertyChanged(nameof(this.Name));
                ProjectItem.Save(this, this.DirectoryPath);
            }
        }

        /// <summary>
        /// Gets or sets the description for this object.
        /// </summary>
        [DataMember]
        public virtual String Description
        {
            get
            {
                return this.description;
            }

            set
            {
                if (this.description == value)
                {
                    return;
                }

                this.description = value;
                this.RaisePropertyChanged(nameof(this.Description));
                ProjectItem.Save(this, this.DirectoryPath);
            }
        }

        /// <summary>
        /// Gets or sets the hotkey for this project item.
        /// </summary>
        public virtual Hotkey HotKey
        {
            get
            {
                return this.hotkey;
            }

            set
            {
                if (this.hotkey == value)
                {
                    return;
                }

                this.hotkey = value;
                this.HotKey?.SetCallBackFunction(() => this.IsActivated = !this.IsActivated);
                this.RaisePropertyChanged(nameof(this.HotKey));
                ProjectItem.Save(this, this.DirectoryPath);
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of this project item.
        /// </summary>
        [DataMember]
        [Browsable(false)]
        public Guid Guid
        {
            get
            {
                return this.guid;
            }

            set
            {
                if (this.guid == value)
                {
                    return;
                }

                this.guid = value;
                this.RaisePropertyChanged(nameof(this.Guid));
                ProjectItem.Save(this, this.DirectoryPath);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this item is activated.
        /// </summary>
        [Browsable(false)]
        public Boolean IsActivated
        {
            get
            {
                return this.isActivated;
            }

            set
            {
                lock (this.ActivationLock)
                {
                    if (this.isActivated == value)
                    {
                        return;
                    }

                    // Change activation state
                    Boolean previousValue = this.isActivated;
                    this.isActivated = value;
                    this.OnActivationChanged();

                    // Activation failed
                    if (this.isActivated == previousValue)
                    {
                        return;
                    }

                    this.RaisePropertyChanged(nameof(this.IsActivated));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating if this project item is enabled.
        /// </summary>
        [Browsable(false)]
        public virtual Boolean IsEnabled
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the display value to represent this project item.
        /// </summary>
        public virtual String DisplayValue
        {
            get
            {
                return String.Empty;
            }

            set
            {
            }
        }

        /// <summary>
        /// Gets or sets a lock for activating project items.
        /// </summary>
        private Object ActivationLock { get; set; }

        /// <summary>
        /// Invoked when this object is deserialized.
        /// </summary>
        /// <param name="streamingContext">Streaming context.</param>
        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            if (this.Guid == null || this.Guid == Guid.Empty)
            {
                this.guid = Guid.NewGuid();
            }

            this.ActivationLock = new Object();
        }

        /// <summary>
        /// Updates event for this project item.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Clones the project item.
        /// </summary>
        /// <returns>The clone of the project item.</returns>
        public virtual ProjectItem Clone()
        {
            Byte[] serializedProjectItem;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ProjectItem));

            // Serialize this project item
            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, this);
                serializedProjectItem = memoryStream.ToArray();
            }

            // Deserialize this project item to clone it
            using (MemoryStream memoryStream = new MemoryStream(serializedProjectItem))
            {
                return serializer.ReadObject(memoryStream) as ProjectItem;
            }
        }

        /// <summary>
        /// Updates the hotkey, bypassing setters to avoid triggering view updates.
        /// </summary>
        /// <param name="hotkey">The hotkey for this project item.</param>
        public void LoadHotkey(Hotkey hotkey)
        {
            this.hotkey = hotkey;

            this.HotKey?.SetCallBackFunction(() => this.IsActivated = !this.IsActivated);
        }

        public void Dispose()
        {
            this.HotKey?.Dispose();
        }

        /// <summary>
        /// Randomizes the guid of this project item.
        /// </summary>
        public void ResetGuid()
        {
            this.Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Event received when a key is released.
        /// </summary>
        /// <param name="key">The key that was released.</param>
        public void OnKeyPress(Key key)
        {
        }

        /// <summary>
        /// Event received when a key is down.
        /// </summary>
        /// <param name="key">The key that is down.</param>
        public void OnKeyRelease(Key key)
        {
        }

        /// <summary>
        /// Event received when a key is down.
        /// </summary>
        /// <param name="key">The key that is down.</param>
        public void OnKeyDown(Key key)
        {
        }

        /// <summary>
        /// Event received when a set of keys are down.
        /// </summary>
        /// <param name="pressedKeys">The down keys.</param>
        public void OnUpdateAllDownKeys(HashSet<Key> pressedKeys)
        {
            if (this.HotKey is KeyboardHotkey)
            {
                KeyboardHotkey keyboardHotkey = this.HotKey as KeyboardHotkey;
            }
        }

        /// <summary>
        /// Indicates that a given property in this project item has changed.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void RaisePropertyChanged(String propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Deactivates this item without triggering the <see cref="OnActivationChanged" /> function.
        /// </summary>
        protected void ResetActivation()
        {
            lock (this.ActivationLock)
            {
                this.isActivated = false;
                this.RaisePropertyChanged(nameof(this.IsActivated));
            }
        }

        /// <summary>
        /// Called when the activation state changes.
        /// </summary>
        protected virtual void OnActivationChanged()
        {
        }

        /// <summary>
        /// Overridable function indicating if this script can be activated.
        /// </summary>
        /// <returns>True if the script can be activated, otherwise false.</returns>
        protected virtual Boolean IsActivatable()
        {
            return true;
        }
    }
    //// End class
}
//// End namespace