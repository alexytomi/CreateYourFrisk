// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Google.Android.AppBundle.Editor.Internal.Config
{
    [Serializable]
    public class SerializableAssetBundle
    {
        public string path;

        public string textureCompressionFormat = TextureCompressionFormat.Default.ToString();

        public string deviceTier;

        public string deviceGroup;


        public TextureCompressionFormat TextureCompressionFormat
        {
            get { return SerializationHelper.GetTextureCompressionFormat(textureCompressionFormat); }
            set { textureCompressionFormat = value.ToString(); }
        }

        public DeviceTier DeviceTier
        {
            get { return SerializationHelper.GetDeviceTier(deviceTier); }
            set { deviceTier = value != null ? value.ToString() : null; }
        }

    }
}