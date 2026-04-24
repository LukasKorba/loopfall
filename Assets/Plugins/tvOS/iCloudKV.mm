#import <Foundation/Foundation.h>

// NSUbiquitousKeyValueStore bridge for Unity. Shared 1 MB KV store across
// a user's iOS / tvOS / macOS devices signed into the same iCloud account.
// Exposed via DllImport("__Internal") from C#; see ICloudKVStore.cs.

// extern "C": this file is compiled as Objective-C++ (.mm), so without it the symbol
// gets C++ name mangling (__Z16UnitySendMessage…) which doesn't match Unity's plain C export.
// weak_import: Unity's macOS UnityPlayer.dylib does NOT export UnitySendMessage (only iOS
// does, via static linking). Without weak_import the macOS .bundle fails to dlopen at all.
// With it, the symbol resolves to NULL on macOS and we null-check before calling; Mac users
// lose real-time external-change notifications (iCloud push from another device while app is
// open), but the pull-merge-push-on-launch path still works — which is the critical one.
extern "C" void UnitySendMessage(const char *obj, const char *method, const char *msg) __attribute__((weak_import));

static NSString *gCallbackObject = nil;
static NSString *gLastReturnedString = nil; // keeps the UTF8 pointer valid until next Get

@interface LoopfallICloudObserver : NSObject
- (void)cloudChanged:(NSNotification *)note;
@end

@implementation LoopfallICloudObserver
- (void)cloudChanged:(NSNotification *)note {
    if (gCallbackObject != nil && UnitySendMessage != NULL) {
        UnitySendMessage([gCallbackObject UTF8String], "OnCloudChangedExternally", "");
    }
}
@end

static LoopfallICloudObserver *gObserver = nil;

extern "C" {

void _iCloudKV_Init(const char* callbackObject) {
    if (callbackObject != NULL) {
        gCallbackObject = [[NSString alloc] initWithUTF8String:callbackObject];
    }
    if (gObserver == nil) {
        gObserver = [[LoopfallICloudObserver alloc] init];
        NSUbiquitousKeyValueStore *store = [NSUbiquitousKeyValueStore defaultStore];
        [[NSNotificationCenter defaultCenter] addObserver:gObserver
            selector:@selector(cloudChanged:)
            name:NSUbiquitousKeyValueStoreDidChangeExternallyNotification
            object:store];
        [store synchronize];
    }
}

void _iCloudKV_SetString(const char* key, const char* value) {
    if (key == NULL) return;
    NSString *k = [NSString stringWithUTF8String:key];
    if (value == NULL) {
        [[NSUbiquitousKeyValueStore defaultStore] removeObjectForKey:k];
    } else {
        NSString *v = [NSString stringWithUTF8String:value];
        [[NSUbiquitousKeyValueStore defaultStore] setString:v forKey:k];
    }
}

const char* _iCloudKV_GetString(const char* key) {
    if (key == NULL) return NULL;
    NSString *k = [NSString stringWithUTF8String:key];
    NSString *v = [[NSUbiquitousKeyValueStore defaultStore] stringForKey:k];
    if (v == nil) return NULL;
    gLastReturnedString = v; // retain so the returned pointer stays valid
    return [v UTF8String];
}

void _iCloudKV_SetLong(const char* key, long long value) {
    if (key == NULL) return;
    NSString *k = [NSString stringWithUTF8String:key];
    [[NSUbiquitousKeyValueStore defaultStore] setLongLong:value forKey:k];
}

long long _iCloudKV_GetLong(const char* key) {
    if (key == NULL) return 0;
    NSString *k = [NSString stringWithUTF8String:key];
    return [[NSUbiquitousKeyValueStore defaultStore] longLongForKey:k];
}

void _iCloudKV_Synchronize() {
    [[NSUbiquitousKeyValueStore defaultStore] synchronize];
}

}
