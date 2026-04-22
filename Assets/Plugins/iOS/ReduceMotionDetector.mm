// Bridge for Apple's system-level "Reduce Motion" accessibility setting.
// iOS / iPadOS / tvOS — UIAccessibilityIsReduceMotionEnabled()
// macOS           — NSWorkspace.accessibilityDisplayShouldReduceMotion
// Returns 0/1 to Unity via DllImport("__Internal"); see AccessibilitySettings.cs.

#import <Foundation/Foundation.h>

#if TARGET_OS_IOS || TARGET_OS_TV
#import <UIKit/UIKit.h>
#elif TARGET_OS_OSX
#import <AppKit/AppKit.h>
#endif

extern "C" {

int _ReduceMotion_IsEnabled() {
#if TARGET_OS_IOS || TARGET_OS_TV
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
#elif TARGET_OS_OSX
    if (@available(macOS 10.12, *)) {
        return [[NSWorkspace sharedWorkspace] accessibilityDisplayShouldReduceMotion] ? 1 : 0;
    }
    return 0;
#else
    return 0;
#endif
}

}
