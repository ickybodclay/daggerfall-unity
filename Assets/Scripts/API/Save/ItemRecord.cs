﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2016 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect.Utility;

namespace DaggerfallConnect.Save
{
    /// <summary>
    /// Item record.
    /// SaveTreeRecordTypes = 0x02
    /// </summary>
    public class ItemRecord : SaveTreeBaseRecord
    {
        #region Fields

        ItemRecordData parsedData;

        #endregion

        #region Properties

        public ItemRecordData ParsedData
        {
            get { return parsedData; }
            set { parsedData = value; }
        }

        #endregion

        #region Structures

        /// <summary>
        /// Stores native item data exactly as read from save file.
        /// </summary>
        public struct ItemRecordData
        {
            public string name;
            public UInt16 category1;
            public UInt16 category2;
            public UInt32 value1;
            public UInt32 value2;
            public UInt16 hits1;
            public UInt16 hits2;
            public UInt16 hits3;
            public UInt32 image1;                   // Inventory list and equip image
            public UInt32 image2;                   // Seems to be a generic "junk" image
            public UInt16 material;
            public Byte color;
            public UInt32 weight;
            public UInt16 enchantmentPoints;
            public UInt32 message;
            public UInt16[] magic;
        }

        #endregion

        #region Constructors

        public ItemRecord()
        {
        }

        public ItemRecord(BinaryReader reader, int length)
            : base(reader, length)
        {
            ReadNativeItemData();
        }

        #endregion

        #region Public Methods

        public void CopyTo(ItemRecord other)
        {
            // Copy base record data
            base.CopyTo(other);

            // Copy item data
            other.parsedData = this.parsedData;
        }

        #endregion

        #region Private Methods

        void ReadNativeItemData()
        {
            // Must be an item type
            if (recordType != RecordTypes.Item)
                return;

            // Prepare stream
            MemoryStream stream = new MemoryStream(RecordData);
            BinaryReader reader = new BinaryReader(stream);

            // Read native item data
            parsedData = new ItemRecordData();
            parsedData.name = FileProxy.ReadCString(reader, 32);
            parsedData.category1 = reader.ReadUInt16();
            parsedData.category2 = reader.ReadUInt16();
            parsedData.value1 = reader.ReadUInt32();
            parsedData.value2 = reader.ReadUInt32();
            parsedData.hits1 = reader.ReadUInt16();
            parsedData.hits2 = reader.ReadUInt16();
            parsedData.hits3 = reader.ReadUInt16();
            parsedData.image1 = reader.ReadUInt16();
            parsedData.image2 = reader.ReadUInt16();
            parsedData.material = reader.ReadUInt16();
            parsedData.color = reader.ReadByte();
            parsedData.weight = reader.ReadUInt32();
            parsedData.enchantmentPoints = reader.ReadUInt16();
            parsedData.message = reader.ReadUInt32();

            // Read magic effect array
            const int effectCount = 10;
            parsedData.magic = new ushort[effectCount];
            for (int i = 0; i < effectCount; i++)
            {
                parsedData.magic[i] = reader.ReadUInt16();
            }

            // Close stream
            reader.Close();
        }

        #endregion
    }
}