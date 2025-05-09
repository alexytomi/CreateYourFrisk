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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Android.AppBundle.Editor.Internal.AssetPacks;
using UnityEditor;

namespace Google.Android.AppBundle.Editor
{
    /// <summary>
    /// Provides helper methods for building AssetBundles with various texture compression formats.
    /// </summary>
    public static class AssetBundleBuilder
    {
        /// <summary>
        /// Run one or more AssetBundle builds with the specified texture compression formats.
        /// Notes about the <see cref="outputPath"/> parameter:
        /// - If a relative path is provided, the file paths in the returned AssetPackConfig will be relative paths.
        /// - If an absolute path is provided, the file paths in the returned object will be absolute paths.
        /// - AssetBundle builds for additional texture formats will be created in siblings of this directory. For
        ///   example, for outputDirectory "a/b/c" and texture format ASTC, there will be a directory "a/b/c#tcf_astc".
        /// - If allowClearDirectory is false, this directory and any sibling directories must be empty or not exist,
        ///   otherwise an exception is thrown.
        /// </summary>
        /// <param name="outputPath">The output directory for the ETC1 AssetBundles. See other notes above.</param>
        /// <param name="builds">The main argument to <see cref="BuildPipeline"/>.</param>
        /// <param name="deliveryMode">A delivery mode to apply to every asset pack in the generated config.</param>
        /// <param name="additionalTextureFormats">Texture formats to build for in addition to ETC1.</param>
        /// <param name="assetBundleOptions">Options to pass to <see cref="BuildPipeline"/>.</param>
        /// <param name="allowClearDirectory">Allows this method to clear the contents of the output directory.</param>
        /// <returns>An <see cref="AssetPackConfig"/> containing file paths to all generated AssetBundles.</returns>
        public static AssetPackConfig BuildAssetBundles(
            string outputPath,
            AssetBundleBuild[] builds,
            AssetPackDeliveryMode deliveryMode,
            IEnumerable<MobileTextureSubtarget> additionalTextureFormats = null,
            BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle,
            bool allowClearDirectory = false)
        {
            var nameToTextureFormatToPath = BuildAssetBundles(outputPath, builds, assetBundleOptions,
                MobileTextureSubtarget.ETC, additionalTextureFormats, allowClearDirectory);
            var assetPackConfig = new AssetPackConfig();
            foreach (var compressionToPath in nameToTextureFormatToPath.Values)
            {
                assetPackConfig.AddAssetBundles(compressionToPath, deliveryMode);
            }

            return assetPackConfig;
        }

        /// <summary>
        /// Run one or more AssetBundle builds with the specified texture compression formats.
        /// This variant allows for overriding the base format and provides a different return type.
        /// </summary>
        /// <param name="outputPath">The output directory for base AssetBundles. See the other method for notes.</param>
        /// <param name="builds">The main argument to <see cref="BuildPipeline"/>.</param>
        /// <param name="assetBundleOptions">Options to pass to <see cref="BuildPipeline"/>.</param>
        /// <param name="baseTextureFormat">The default format. Note: ETC1 is supported by all devices.</param>
        /// <param name="additionalTextureFormats">Texture formats to generate for in addition to the base.</param>
        /// <param name="allowClearDirectory">Allows this method to clear the contents of the output directory.</param>
        /// <returns>A dictionary from AssetBundle name to TextureCompressionFormat to file outputPath.</returns>
        public static Dictionary<string, Dictionary<TextureCompressionFormat, string>> BuildAssetBundles(
            string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions,
            MobileTextureSubtarget baseTextureFormat, IEnumerable<MobileTextureSubtarget> additionalTextureFormats,
            bool allowClearDirectory)
        {
            if (builds == null || builds.Length == 0)
            {
                throw new ArgumentException("AssetBundleBuild parameter cannot be null or empty");
            }

            foreach (var build in builds)
            {
                if (!AndroidAppBundle.IsValidModuleName(build.assetBundleName))
                {
                    throw new ArgumentException("Invalid AssetBundle name: " + build.assetBundleName);
                }
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            CheckDirectory(outputPath, allowClearDirectory);

            // Make unique and silently remove the base format, if it was present.
            var textureSubtargets = new HashSet<MobileTextureSubtarget>(
                additionalTextureFormats ?? Enumerable.Empty<MobileTextureSubtarget>());
            textureSubtargets.Remove(baseTextureFormat);

            // Note: GetCompressionFormatSuffix() will throw for unsupported formats.
            var paths = textureSubtargets.Select(format => outputPath + GetCompressionFormatSuffix(format));
            foreach (var path in paths)
            {
                CheckDirectory(path, allowClearDirectory);
            }

            // Throws if the base format is invalid.
            GetCompressionFormat(baseTextureFormat);

            return BuildAssetBundlesInternal(
                outputPath, builds, assetBundleOptions, baseTextureFormat, textureSubtargets);
        }

        /// <summary>
        /// Run one or more AssetBundle builds for the specified device tiers.
        /// Notes about the <see cref="outputPath"/> parameter:
        /// - If a relative path is provided, the file paths in the returned AssetPackConfig will be relative paths.
        /// - If an absolute path is provided, the file paths in the returned object will be absolute paths.
        /// - AssetBundle builds for device tiers will be created in siblings of this directory. For
        ///   example, for outputDirectory "a/b/c" and device tier HIGH, there will be a directory "a/b/c#tier_high".
        /// - If allowClearDirectory is false, this directory and any sibling directories must be empty or not exist,
        ///   otherwise an exception is thrown.
        /// </summary>
        /// <param name="outputPath">The output directory for AssetBundles. See other notes above.</param>
        /// <param name="deliveryMode">A delivery mode to apply to every asset pack in the generated config.</param>
        /// <param name="deviceTierToBuilds">A dictionary from Device Tier to asset bundle builds. All device tiers
        /// should contains the same asset bundle names.</param>
        /// <param name="defaultDeviceTier">
        /// Default device tier to be used for standalone APKs. Set to zero if not specified.</param>
        /// <param name="assetBundleOptions">Options to pass to <see cref="BuildPipeline"/>.</param>
        /// <param name="allowClearDirectory">Allows this method to clear the contents of the output directory.</param>
        /// <returns>An <see cref="AssetPackConfig"/> containing file paths to all generated AssetBundles.</returns>
        public static AssetPackConfig BuildAssetBundlesDeviceTier(
            string outputPath,
            AssetPackDeliveryMode deliveryMode,
            Dictionary<DeviceTier, AssetBundleBuild[]> deviceTierToBuilds,
            DeviceTier defaultDeviceTier = null,
            BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle,
            bool allowClearDirectory = false)
        {
            if (defaultDeviceTier == null)
            {
                defaultDeviceTier = DeviceTier.From(0);
            }

            if (!deviceTierToBuilds.Keys.Contains(defaultDeviceTier))
            {
                throw new ArgumentException("Default device tier not present in deviceTierToBuilds.");
            }

            var nameToDeviceTierToPath = BuildAssetBundlesDeviceTier(outputPath, deviceTierToBuilds,
                assetBundleOptions, allowClearDirectory);
            var assetPackConfig = new AssetPackConfig();
            foreach (var deviceTierToPath in nameToDeviceTierToPath.Values)
            {
                assetPackConfig.AddAssetBundles(deviceTierToPath, deliveryMode);
            }

            assetPackConfig.DefaultDeviceTier = defaultDeviceTier;
            return assetPackConfig;
        }

        /// <summary>
        /// Run one or more AssetBundle builds for each device tier.
        /// </summary>
        /// <param name="outputPath">The output directory for base AssetBundles. See the other method for notes.</param>
        /// <param name="deviceTierToBuilds">A dictionary from Device Tier to asset bundle builds. All device tiers
        /// should contains the same asset bundle names.</param>
        /// <param name="assetBundleOptions">Options to pass to <see cref="BuildPipeline"/>.</param>
        /// <param name="allowClearDirectory">Allows this method to clear the contents of the output directory.</param>
        /// <returns>A dictionary from AssetBundle name to DeviceTier to file outputPath.</returns>
        public static Dictionary<string, Dictionary<DeviceTier, string>> BuildAssetBundlesDeviceTier(
            string outputPath, Dictionary<DeviceTier, AssetBundleBuild[]> deviceTierToBuilds,
            BuildAssetBundleOptions assetBundleOptions,
            bool allowClearDirectory)
        {
            if (deviceTierToBuilds == null || deviceTierToBuilds.Count == 0)
            {
                throw new ArgumentException("DeviceTierToBuild parameter cannot be null or empty");
            }

            if (deviceTierToBuilds.Values.Any(builds => builds == null || builds.Length == 0))
            {
                throw new ArgumentException("AssetBundleBuilds value cannot be null or empty");
            }

            var assetBundleNames = deviceTierToBuilds.Values.First().Select(build => build.assetBundleName).ToList();

            if (deviceTierToBuilds.Values.Any(builds =>
                    !(builds.Length == assetBundleNames.Count() &&
                      builds.Select(build => build.assetBundleName).Except(assetBundleNames).ToList().Count == 0)))
            {
                throw new ArgumentException("AssetBundleBuild names cannot differ across device tiers.");
            }

            foreach (var assetBundleName in assetBundleNames)
            {
                if (!AndroidAppBundle.IsValidModuleName(assetBundleName))
                {
                    throw new ArgumentException("Invalid AssetBundle name: " + assetBundleName);
                }
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            CheckDirectory(outputPath, allowClearDirectory);

            var tiers = new HashSet<DeviceTier>(deviceTierToBuilds.Keys);
            var paths = tiers.Select(tier => outputPath + DeviceTierTargetingTools.GetTargetingSuffix(tier));
            foreach (var path in paths)
            {
                CheckDirectory(path, allowClearDirectory);
            }

            return BuildAssetBundlesDeviceTierInternal(
                outputPath, assetBundleOptions, deviceTierToBuilds, assetBundleNames);
        }

        /// <summary>
        /// Run one or more AssetBundle builds for the specified device groups.
        /// Notes about the <see cref="outputPath"/> parameter:
        /// - If a relative path is provided, the file paths in the returned AssetPackConfig will be relative paths.
        /// - If an absolute path is provided, the file paths in the returned object will be absolute paths.
        /// - AssetBundle builds for device groups will be created in siblings of this directory. For
        ///   example, for outputDirectory "a/b/c" and device group X, there will be a directory "a/b/c#group_X".
        /// - If allowClearDirectory is false, this directory and any sibling directories must be empty or not exist,
        ///   otherwise an exception is thrown.
        /// </summary>
        /// <param name="outputPath">The output directory for AssetBundles. See other notes above.</param>
        /// <param name="deliveryMode">A delivery mode to apply to every asset pack in the generated config.</param>
        /// <param name="deviceGroupToBuilds">A dictionary from Device Group to asset bundle builds. All device groups
        /// should contain the same asset bundle names.</param>
        /// <param name="defaultDeviceGroup">
        /// Default device group to be used for standalone APKs. Set to null if not specified.</param>
        /// <param name="assetBundleOptions">Options to pass to <see cref="BuildPipeline"/>.</param>
        /// <param name="allowClearDirectory">Allows this method to clear the contents of the output directory.</param>
        /// <returns>An <see cref="AssetPackConfig"/> containing file paths to all generated AssetBundles.</returns>
        public static AssetPackConfig BuildAssetBundlesDeviceGroup(
            string outputPath,
            AssetPackDeliveryMode deliveryMode,
            Dictionary<string, AssetBundleBuild[]> deviceGroupToBuilds,
            string defaultDeviceGroup = "other",
            BuildAssetBundleOptions assetBundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle,
            bool allowClearDirectory = false)
        {
            var nameToDeviceGroupToPath = BuildAssetBundlesDeviceGroup(outputPath, deviceGroupToBuilds,
                assetBundleOptions, allowClearDirectory);
            var assetPackConfig = new AssetPackConfig();
            foreach (var deviceGroupToPath in nameToDeviceGroupToPath.Values)
            {
                assetPackConfig.AddAssetBundles(deviceGroupToPath, deliveryMode);
            }

            assetPackConfig.DefaultDeviceGroup = defaultDeviceGroup;
            return assetPackConfig;
        }

        /// <summary>
        /// Run one or more AssetBundle builds for each device group.
        /// </summary>
        /// <param name="outputPath">The output directory for base AssetBundles. See the other method for notes.</param>
        /// <param name="deviceGroupToBuilds">A dictionary from Device Group to asset bundle builds. All device groups
        /// should contains the same asset bundle names.</param>
        /// <param name="assetBundleOptions">Options to pass to <see cref="BuildPipeline"/>.</param>
        /// <param name="allowClearDirectory">Allows this method to clear the contents of the output directory.</param>
        /// <returns>A dictionary from AssetBundle name to device group to file outputPath.</returns>
        public static Dictionary<string, Dictionary<string, string>> BuildAssetBundlesDeviceGroup(
            string outputPath,
            Dictionary<string, AssetBundleBuild[]> deviceGroupToBuilds,
            BuildAssetBundleOptions assetBundleOptions,
            bool allowClearDirectory)
        {
            if (deviceGroupToBuilds == null || deviceGroupToBuilds.Count == 0)
            {
                throw new ArgumentException("deviceGroupToBuilds parameter cannot be null or empty");
            }

            if (deviceGroupToBuilds.Values.Any(builds => builds == null || builds.Length == 0))
            {
                throw new ArgumentException("AssetBundleBuilds value cannot be null or empty");
            }

            var assetBundleNames = deviceGroupToBuilds.Values.First().Select(build => build.assetBundleName).ToList();

            if (deviceGroupToBuilds.Values.Any(builds =>
                    !(builds.Length == assetBundleNames.Count() &&
                      builds.Select(build => build.assetBundleName).Except(assetBundleNames).ToList().Count == 0)))
            {
                throw new ArgumentException("AssetBundleBuild names cannot differ across device groups.");
            }

            foreach (var assetBundleName in assetBundleNames)
            {
                if (!AndroidAppBundle.IsValidModuleName(assetBundleName))
                {
                    throw new ArgumentException("Invalid AssetBundle name: " + assetBundleName);
                }
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            CheckDirectory(outputPath, allowClearDirectory);

            var groups = new HashSet<string>(deviceGroupToBuilds.Keys);
            var paths = groups.Select(group => outputPath + DeviceGroupTargetingTools.GetTargetingSuffix(group));
            foreach (var path in paths)
            {
                CheckDirectory(path, allowClearDirectory);
            }

            return BuildAssetBundlesDeviceGroupInternal(
                outputPath, assetBundleOptions, deviceGroupToBuilds, assetBundleNames);
        }


        // The internal method assumes all parameter preconditions have already been checked.
        private static Dictionary<string, Dictionary<TextureCompressionFormat, string>> BuildAssetBundlesInternal(
            string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions,
            MobileTextureSubtarget baseTextureFormat, IEnumerable<MobileTextureSubtarget> textureSubtargets)
        {
            // Use a dictionary to capture the generated AssetBundles' file names.
            var nameToTextureFormatToPath = builds.ToDictionary(
                build => build.assetBundleName, _ => new Dictionary<TextureCompressionFormat, string>());

            var originalAndroidBuildSubtarget = EditorUserBuildSettings.androidBuildSubtarget;
            try
            {
                // First build for the additional texture formats.
                foreach (var textureSubtarget in textureSubtargets)
                {
                    // Build AssetBundles in the the base format's directory.
                    EditorUserBuildSettings.androidBuildSubtarget = textureSubtarget;
                    BuildAssetBundles(outputPath, builds, assetBundleOptions);

                    // Then move the files to a new directory with the compression format suffix.
                    var outputPathWithSuffix = outputPath + GetCompressionFormatSuffix(textureSubtarget);
                    Directory.Move(outputPath, outputPathWithSuffix);

                    var textureCompressionFormat = GetCompressionFormat(textureSubtarget);
                    UpdateDictionary(nameToTextureFormatToPath, outputPathWithSuffix, textureCompressionFormat);
                }

                // Then build for the base format.
                EditorUserBuildSettings.androidBuildSubtarget = baseTextureFormat;
                BuildAssetBundles(outputPath, builds, assetBundleOptions);

                UpdateDictionary(nameToTextureFormatToPath, outputPath, TextureCompressionFormat.Default);
                return nameToTextureFormatToPath;
            }
            finally
            {
                EditorUserBuildSettings.androidBuildSubtarget = originalAndroidBuildSubtarget;
            }
        }

        private static Dictionary<string, Dictionary<DeviceTier, string>> BuildAssetBundlesDeviceTierInternal(
            string outputPath, BuildAssetBundleOptions assetBundleOptions,
            Dictionary<DeviceTier, AssetBundleBuild[]> deviceTierToBuilds, IEnumerable<string> assetBundleNames)
        {
            // Use a dictionary to capture the generated AssetBundles' file names.
            var nameToDeviceTierToPath = assetBundleNames.ToDictionary(
                bundleName => bundleName, _ => new Dictionary<DeviceTier, string>());
            foreach (var deviceTier in deviceTierToBuilds.Keys)
            {
                var tierBuilds = deviceTierToBuilds[deviceTier];
                // Build AssetBundles in the the base format's directory.
                BuildAssetBundles(outputPath, tierBuilds, assetBundleOptions);

                // Then move the files to a new directory with the device tier suffix.
                var outputPathWithSuffix = outputPath + DeviceTierTargetingTools.GetTargetingSuffix(deviceTier);
                Directory.Move(outputPath, outputPathWithSuffix);

                UpdateDictionaryDeviceTier(nameToDeviceTierToPath, outputPathWithSuffix, deviceTier);
            }

            return nameToDeviceTierToPath;
        }

        private static void UpdateDictionaryDeviceTier(
            Dictionary<string, Dictionary<DeviceTier, string>> nameToDeviceTierToPath,
            string outputPath, DeviceTier deviceTier)
        {
            foreach (var entry in nameToDeviceTierToPath)
            {
                var filePath = Path.Combine(outputPath, entry.Key);
                entry.Value[deviceTier] = filePath;
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException(string.Format("Missing AssetBundle file: " + filePath));
                }
            }
        }

        private static Dictionary<string, Dictionary<string, string>> BuildAssetBundlesDeviceGroupInternal(
            string outputPath, BuildAssetBundleOptions assetBundleOptions,
            Dictionary<string, AssetBundleBuild[]> deviceGroupToBuilds, IEnumerable<string> assetBundleNames)
        {
            // Use a dictionary to capture the generated AssetBundles' file names.
            var nameToDeviceGroupToPath = assetBundleNames.ToDictionary(
                bundleName => bundleName, _ => new Dictionary<string, string>());
            foreach (var deviceGroup in deviceGroupToBuilds.Keys)
            {
                var groupBuilds = deviceGroupToBuilds[deviceGroup];
                // Build AssetBundles in the the base format's directory.
                BuildAssetBundles(outputPath, groupBuilds, assetBundleOptions);

                // Then move the files to a new directory with the device group suffix.
                var outputPathWithSuffix = outputPath + DeviceGroupTargetingTools.GetTargetingSuffix(deviceGroup);
                Directory.Move(outputPath, outputPathWithSuffix);

                UpdateDictionaryDeviceGroup(nameToDeviceGroupToPath, outputPathWithSuffix, deviceGroup);
            }

            return nameToDeviceGroupToPath;
        }

        private static void UpdateDictionaryDeviceGroup(
            Dictionary<string, Dictionary<string, string>> nameToDeviceGroupToPath,
            string outputPath, string deviceGroup)
        {
            foreach (var entry in nameToDeviceGroupToPath)
            {
                var filePath = Path.Combine(outputPath, entry.Key);
                entry.Value[deviceGroup] = filePath;
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException(string.Format("Missing AssetBundle file: " + filePath));
                }
            }
        }


        private static void BuildAssetBundles(
            string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions)
        {
            // The output directory must exist before calling BuildAssetBundles(), so create it first.
            Directory.CreateDirectory(outputPath);
            var assetPackManifest =
                BuildPipeline.BuildAssetBundles(outputPath, builds, assetBundleOptions, BuildTarget.Android);
            if (assetPackManifest == null)
            {
                throw new InvalidOperationException("Error building AssetBundles");
            }
        }

        private static void UpdateDictionary(
            Dictionary<string, Dictionary<TextureCompressionFormat, string>> nameToTextureFormatToPath,
            string outputPath, TextureCompressionFormat textureCompressionFormat)
        {
            foreach (var entry in nameToTextureFormatToPath)
            {
                var filePath = Path.Combine(outputPath, entry.Key);
                entry.Value[textureCompressionFormat] = filePath;
                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException(string.Format("Missing AssetBundle file: " + filePath));
                }
            }
        }

        private static string GetCompressionFormatSuffix(MobileTextureSubtarget subtarget)
        {
            var format = GetCompressionFormat(subtarget);
            return TextureTargetingTools.GetTargetingSuffix(format);
        }

        private static TextureCompressionFormat GetCompressionFormat(MobileTextureSubtarget subtarget)
        {
            switch (subtarget)
            {
                case MobileTextureSubtarget.ASTC:
                    return TextureCompressionFormat.Astc;
                case MobileTextureSubtarget.DXT:
                    return TextureCompressionFormat.Dxt1;
                case MobileTextureSubtarget.ETC:
                    return TextureCompressionFormat.Etc1;
                case MobileTextureSubtarget.ETC2:
                    return TextureCompressionFormat.Etc2;
                case MobileTextureSubtarget.PVRTC:
                    return TextureCompressionFormat.Pvrtc;
                default:
                    throw new ArgumentException("Unsupported MobileTextureSubtarget: " + subtarget);
            }
        }

        private static void CheckDirectory(string path, bool allowClearDirectory)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                return;
            }

            if (!allowClearDirectory && directoryInfo.GetFileSystemInfos().Any())
            {
                throw new ArgumentException("Directory is not empty: " + path);
            }

            directoryInfo.Delete( /* recursive= */ true);
        }
    }
}