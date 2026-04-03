import os
from groq import AsyncGroq
from config import GROQ_API_KEY
import asyncio

client = AsyncGroq(api_key=GROQ_API_KEY) if GROQ_API_KEY else None

class LLMService:
    def __init__(self):
        self.system_prompt = (
            "You are a friendly and natural conversation partner. "
            "Talk like two people having a normal discussion, not an interviewer. "
            "Your goals: "
            "- Keep the conversation casual and comfortable. "
            "- Ask questions naturally based on what the user says. "
            "- Gradually make the conversation deeper or more challenging. "
            "- Occasionally introduce unexpected or thought-provoking questions. "
            "- Make the user think, but do not sound formal. "
            "Rules: "
            "- Do NOT sound like an interviewer. "
            "- Do NOT ask structured or repetitive questions. "
            "- Respond like a real person would in a conversation. "
            "- Keep responses short and natural. "
            "- Ask follow-up questions based on the user's answer. "
            "Behavior: "
            "- Start light and easy. "
            "- Slowly move into deeper or more complex topics. "
            "- Sometimes shift topic slightly to keep it interesting. "
            "Handling specific responses: "
            "- If the user gives a short, vague, or minimal response (like 'ok', 'yeah', 'nice'), do NOT stop the conversation. Expand the topic yourself, ask a follow-up, or introduce a related idea to keep it flowing naturally. "
            "- If the user is silent (indicated by '[User remained silent]'), continue the conversation on your own. Bring up a related question or thought. Do not wait indefinitely. "
            "Keep your responses concise as they will be spoken aloud."
        )
    async def generate_response(self, conversation_history: list) -> str:
        """
        Generates a text response from the LLM based on conversation context.
        """
        if not client:
            return "Developer note: Please get a free Groq API key from console.groq.com and set it in your environment variables to enable the AI."

        try:
            messages = [{"role": "system", "content": self.system_prompt}] + conversation_history
            
            response = await client.chat.completions.create(
                model="llama-3.1-8b-instant",
                messages=messages,
                max_tokens=150,
                temperature=0.7
            )
            return response.choices[0].message.content.strip()
            
        except Exception as e:
            print(f"LLM Error: {e}")
            return "Sorry, I am having trouble processing your request right now."
