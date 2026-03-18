from datetime import datetime, timedelta
from typing import Dict, List, Any

class SessionManager:
    def __init__(self, session_timeout_minutes=30):
        # Maps session_id to list of message dictionaries: {"role": "user"/"assistant", "content": "text"}
        self.sessions: Dict[str, Dict[str, Any]] = {}
        self.session_timeout = timedelta(minutes=session_timeout_minutes)

    def get_context(self, session_id: str) -> List[dict]:
        """
        Retrieve conversation history for a session.
        """
        self._cleanup_expired_sessions()
        
        if session_id not in self.sessions:
            self.sessions[session_id] = {
                "history": [],
                "last_active": datetime.now()
            }
            
        self.sessions[session_id]["last_active"] = datetime.now()
        return self.sessions[session_id]["history"]
        
    def add_message(self, session_id: str, role: str, content: str):
        """
        Add a message to the session's context.
        """
        context = self.get_context(session_id)
        context.append({"role": role, "content": content})
        
        # Keep context window manageable (e.g., last 10 messages)
        if len(self.sessions[session_id]["history"]) > 10:
            # retain the last 10
            self.sessions[session_id]["history"] = self.sessions[session_id]["history"][-10:]

    def _cleanup_expired_sessions(self):
        """
        Remove sessions that have been inactive for too long.
        """
        now = datetime.now()
        expired_keys = [
            sid for sid, data in self.sessions.items() 
            if now - data["last_active"] > self.session_timeout
        ]
        for sid in expired_keys:
            self.sessions.pop(sid, None)
