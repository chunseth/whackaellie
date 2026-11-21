#import <UIKit/UIKit.h>

// Native side of Haptics.cs. Styles: 0 light, 1 medium, 2 heavy, 3 error.
// Generators are created once and reused, so this works with or without ARC.
static UIImpactFeedbackGenerator *_waeLight = nil;
static UIImpactFeedbackGenerator *_waeMedium = nil;
static UIImpactFeedbackGenerator *_waeHeavy = nil;
static UINotificationFeedbackGenerator *_waeNotify = nil;

extern "C" void _WAE_PlayHaptic(int style)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (_waeLight == nil)
        {
            _waeLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            _waeMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            _waeHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            _waeNotify = [[UINotificationFeedbackGenerator alloc] init];
        }

        switch (style)
        {
            case 1: [_waeMedium impactOccurred]; break;
            case 2: [_waeHeavy impactOccurred]; break;
            case 3: [_waeNotify notificationOccurred:UINotificationFeedbackTypeError]; break;
            default: [_waeLight impactOccurred]; break;
        }
    });
}
