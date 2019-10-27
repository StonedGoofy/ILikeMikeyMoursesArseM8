namespace MSCOMessages.Messages {
	[NetMessageDesc(MessageIds.PickupableSetPosition)]
	class PickupableSetPositionMessage {
		int id;
		Vector3Message position;
	}
}
