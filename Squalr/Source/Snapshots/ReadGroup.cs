﻿namespace Squalr.Source.Snapshots
{
    using Squalr.Properties;
    using Squalr.Source.Results;
    using SqualrCore.Source.Engine;
    using SqualrCore.Source.Engine.Types;
    using SqualrCore.Source.Engine.VirtualMemory;
    using SqualrCore.Source.Utils.Extensions;
    using System;
    using System.Collections.Generic;

    internal class ReadGroup : NormalizedRegion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadGroup" /> class.
        /// </summary>
        /// <param name="baseAddress">The base address of this memory region.</param>
        /// <param name="regionSize">The size of this memory region.</param>
        public ReadGroup(IntPtr baseAddress, UInt64 regionSize) : base(baseAddress, regionSize)
        {
            this.Alignment = SettingsViewModel.GetInstance().Alignment;
            this.ElementDataType = ScanResultsViewModel.GetInstance().ActiveType;

            this.SnapshotRegions = new List<SnapshotRegion>() { new SnapshotRegion(this, 0, regionSize) };
        }

        /// <summary>
        /// Gets the most recently read values.
        /// </summary>
        public unsafe Byte[] CurrentValues { get; private set; }

        /// <summary>
        /// Gets the previously read values.
        /// </summary>
        public unsafe Byte[] PreviousValues { get; private set; }

        /// <summary>
        /// Gets or sets the data type of the elements of this region.
        /// </summary>
        public DataType ElementDataType { get; set; }

        /// <summary>
        /// Gets the element labels.
        /// </summary>
        public unsafe Object[] ElementLabels { get; private set; }

        /// <summary>
        /// Gets or sets the data type of the labels of this region.
        /// </summary>
        public DataType LabelDataType { get; set; }

        public IList<SnapshotRegion> ResultRegions { get; set; }

        public IEnumerable<SnapshotRegion> SnapshotRegions;

        /// <summary>
        /// Reads all memory for this memory region.
        /// </summary>
        /// <returns>The bytes read from memory.</returns>
        public Boolean ReadAllMemory()
        {
            Boolean readSuccess;
            Byte[] newCurrentValues = EngineCore.GetInstance().VirtualMemory.ReadBytes(this.BaseAddress, this.RegionSize.ToInt32(), out readSuccess);

            if (readSuccess)
            {
                this.SetPreviousValues(this.CurrentValues);
                this.SetCurrentValues(newCurrentValues);
            }
            else
            {
                this.SetPreviousValues(null);
                this.SetCurrentValues(null);
            }

            return readSuccess;
        }

        /// <summary>
        /// Determines if a relative comparison can be done for this region, ie current and previous values are loaded.
        /// </summary>
        /// <returns>True if a relative comparison can be done for this region.</returns>
        public Boolean CanCompare(Boolean hasRelativeConstraint)
        {
            if (this?.CurrentValues == null || (hasRelativeConstraint && this?.PreviousValues == null))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the current values of this region.
        /// </summary>
        /// <param name="newValues">The raw bytes of the values.</param>
        public void SetCurrentValues(Byte[] newValues)
        {
            this.CurrentValues = newValues;
        }

        /// <summary>
        /// Sets the previous values of this region.
        /// </summary>
        /// <param name="newValues">The raw bytes of the values.</param>
        public void SetPreviousValues(Byte[] newValues)
        {
            this.PreviousValues = newValues;
        }

        /// <summary>
        /// Sets the element labels for this snapshot region.
        /// </summary>
        /// <param name="newLabels">The new labels to be assigned.</param>
        public void SetElementLabels(Object[] newLabels)
        {
            this.ElementLabels = newLabels;
        }
    }
    //// End class
}
//// End namespace