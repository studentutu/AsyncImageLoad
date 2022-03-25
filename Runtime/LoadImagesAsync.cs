﻿using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityImageLoader.Cache;

namespace UnityImageLoader
{
	/// <summary>
	/// Main class to load images. First setup _discCache. Later use LoadImageAsync
	/// </summary>
	public class LoadImagesAsync : IDisposable
	{
		private bool _disposed = false;
		private AbstractDiscCache _discCache = null;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				DisposeFull();
			}
		}

		private void DisposeFull()
		{
			_discCache?.Dispose();
		}

		public LoadImagesAsync SetDiskLoader(AbstractDiscCache diskCache)
		{
			_discCache = diskCache;
			return this;
		}

		/// <summary>
		/// Will load fetch texture asynchronous. Using MRTK.Rest, File system to retrieve/store data.
		/// </summary>
		public async UniTask<Texture2D> LoadImageAsync(
			Texture2D errorTexture,
			string url,
			Dictionary<string, string> headers,
			CancellationToken token)
		{
			if (!_discCache.FileExists(url))
			{
				var bytes = await LoadBytesViaUrl(url, headers);
				if (bytes != null && !token.IsCancellationRequested)
				{
					_discCache.Set(url, bytes);
				}
			}

			var texture = await TextureExtensions.LoadTextureAsync(_discCache.GetPath(url), token);
			if (texture == null)
			{
				texture = errorTexture;
			}

			return texture;
		}

		public async UniTask<byte[]> LoadBytesViaUrl(string url, Dictionary<string, string> headers)
		{
			var response = await Rest.GetAsync(url, headers, 20, null, true);
			if (!response.Successful)
			{
#pragma warning disable CS0618
				Debug.LogError(response.ResponseCode + ":" + response.ResponseBody);
#pragma warning restore CS0618
				return null;
			}

			if (response.ResponseData != null)
			{
				return response.ResponseData;
			}

			var imageBase64 = await response.GetResponseBody();
			if (!string.IsNullOrEmpty(imageBase64))
			{
				return Convert.FromBase64String(imageBase64);
			}

			return null;
		}
	}
}