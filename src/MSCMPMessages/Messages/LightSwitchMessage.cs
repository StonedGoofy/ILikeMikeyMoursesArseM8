﻿namespace MSCOMessages.Messages {
	[NetMessageDesc(MessageIds.LightSwitch)]
	class LightSwitchMessage {
		Vector3Message pos;
		bool toggle;
	}
}
