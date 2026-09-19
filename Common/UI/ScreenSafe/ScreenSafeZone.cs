#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Dessentials.Common.UI
{
	public class ScreenSafeZone : MonoBehaviour
	{
		public static Action OnOffsetChanged;
		public Canvas canvas;
		public RectTransform[] safeRects;
		[FormerlySerializedAs("fixedTop")]
		public bool fullTop;
		[FormerlySerializedAs("fixedBottom")]
		public bool fullBottom;

		private void Start()
		{
			screenSafeAreas.Add(this);
		}

		private void OnDestroy()
		{
			// Pairs with the Add in Start. Without it the static list keeps a reference to
			// every zone ever spawned, so it grows across scene loads and the offset loops
			// walk a list that is mostly dead entries. Remove is a no-op if Start never ran.
			screenSafeAreas.Remove(this);
		}

		private void OnEnable()
		{
			CheckSafeArea();
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		public void Log()
		{
			var safeArea = Screen.safeArea;
			var sWidth = Screen.currentResolution.width;
			var sHeight = Screen.currentResolution.height;
			var oWidthTop = (Screen.currentResolution.width - safeArea.width - safeArea.x) / 2f;
			var oHeightTop = (Screen.currentResolution.height - safeArea.height - safeArea.y) / 2f;
			var oWidthBot = -safeArea.x / 2f;
			var oHeightBot = -safeArea.y / 2f;
			UnityEngine.Debug.Log($"Screen size: (width:{sWidth}, height:{sHeight})"
				+ $"\nSafe area: {safeArea}"
				+ $"\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})"
				+ $"\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		private void Validate()
		{
			CheckSafeArea();
		}

		private void CheckSafeArea()
		{
			var safeArea = Screen.safeArea;
			safeArea.height -= topBannerOffset;
			if (fullTop)
			{
				safeArea.height = Screen.currentResolution.height - Screen.safeArea.y;
			}
			if (fullBottom)
			{
				safeArea.height += Screen.safeArea.y;
				safeArea.y = 0;
			}
			var anchorMin = safeArea.position;
			var anchorMax = safeArea.position + safeArea.size;

			var sizeDelta = ((RectTransform)canvas.transform).sizeDelta;
			sizeDelta.y = -bottomBannerOffset;
			((RectTransform)canvas.transform).sizeDelta = sizeDelta;
			var position = ((RectTransform)canvas.transform).anchoredPosition;
			position.y = bottomBannerOffset / 2;
			((RectTransform)canvas.transform).anchoredPosition = position;

			var pixelRect = canvas.pixelRect;
			anchorMin.x /= pixelRect.width;
			anchorMin.y /= pixelRect.height;
			anchorMax.x /= pixelRect.width;
			anchorMax.y /= pixelRect.height;

			foreach (var rect in safeRects)
			{
				rect.anchorMin = anchorMin;
				rect.anchorMax = anchorMax;
			}
		}

		private IEnumerator IEValidate()
		{
			Validate();
			yield return null;
			Validate();
		}

#if ODIN_INSPECTOR
		[Button]
#endif
		private void TestTopOffsetForBannerAd(int height) => SetTopOffsetForBannerAd(height);

#if ODIN_INSPECTOR
		[Button]
#endif
		private void TestBottomOffsetForBannerAd(int height) => SetBottomOffsetForBannerAd(height);

		//========================================================================================

		public static float topBannerOffset;
		public static float bottomBannerOffset;
		public static List<ScreenSafeZone> screenSafeAreas = new();

		/// Wipes static state between play sessions. Without this, disabling Reload Domain
		/// in Enter Play Mode Options leaves OnOffsetChanged invoking subscribers from the
		/// previous session, and carries the previous run's banner offsets into the next.
		/// screenSafeAreas is cleared as a backstop; OnDestroy already unregisters each
		/// zone, and the offset loops null-check, so stale entries leak rather than throw.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			OnOffsetChanged = null;
			topBannerOffset = 0f;
			bottomBannerOffset = 0f;
			screenSafeAreas.Clear();
		}

		public static void SetTopOffsetForBannerAd(float pBannerHeight, bool pPlaceInSafeArea = true)
		{
			float offset = 0;
			var safeAreaHeightOffer = Screen.height - Screen.safeArea.height;
			if (!pPlaceInSafeArea)
			{
				if (pBannerHeight <= safeAreaHeightOffer)
					offset = 0;
				else
					offset = pBannerHeight - safeAreaHeightOffer;
			}
			else
				offset = pBannerHeight;

			topBannerOffset = offset;
			foreach (var component in screenSafeAreas)
				if (component != null && component.gameObject.activeSelf)
					component.StartCoroutine(component.IEValidate());
			OnOffsetChanged?.Invoke();
		}
		public static void SetBottomOffsetForBannerAd(float pBannerHeight, bool pPlaceInSafeArea = true)
		{
			float offset = 0;
			var safeAreaHeightOffer = Screen.height - Screen.safeArea.height;
			if (!pPlaceInSafeArea)
			{
				if (pBannerHeight <= safeAreaHeightOffer)
					offset = 0;
				else
					offset = pBannerHeight - safeAreaHeightOffer;
			}
			else
				offset = pBannerHeight;

			bottomBannerOffset = offset;
			foreach (var component in screenSafeAreas)
				if (component != null && component.gameObject.activeSelf)
					component.StartCoroutine(component.IEValidate());
			OnOffsetChanged?.Invoke();
		}
	}
}