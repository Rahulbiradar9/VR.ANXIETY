import os
from groq import AsyncGroq
from config import GROQ_API_KEY
import asyncio

client = AsyncGroq(api_key=GROQ_API_KEY) if GROQ_API_KEY else None

class LLMService:
    def __init__(self):
        self.system_prompt = (
            "You are a helpful and concise conversational assistant. "
            "Keep your responses short, under 3 sentences, as they will be spoken aloud."
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
