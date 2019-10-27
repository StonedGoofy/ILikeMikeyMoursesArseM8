namespace MSCOMessages.Messages {
	[NetMessageDesc(MessageIds.ObjectSyncResponse)]
	class ObjectSyncResponseMessage {

		int objectID;
		bool accepted;
	}
}
