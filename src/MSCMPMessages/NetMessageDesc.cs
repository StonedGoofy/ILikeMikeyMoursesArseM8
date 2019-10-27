using System;


namespace MSCOMessages {
	class NetMessageDesc : Attribute {

		public Messages.MessageIds messageId;

		public NetMessageDesc(Messages.MessageIds id) {
			this.messageId = id;
		}
	}
}
