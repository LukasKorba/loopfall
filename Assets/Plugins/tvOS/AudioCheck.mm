#import <AVFoundation/AVFoundation.h>

extern "C" {
    bool _IsOtherAudioPlaying() {
        return [[AVAudioSession sharedInstance] isOtherAudioPlaying];
    }
}
