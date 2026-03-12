import requests

conversation_history = [
    {
        "role": "system",
        "content": "You are a helpful interview trainer. You ask the candidate interview questions ONLY based on the specific topic they choose. Do NOT ask any questions about their old company or past work experience. Keep your questions concise."
    }
]

def reset_conversation():
    global conversation_history
    conversation_history = [
        {
            "role": "system",
            "content": "You are a helpful interview trainer. You ask the candidate interview questions ONLY based on the specific topic they choose. Do NOT ask any questions about their old company or past work experience. Keep your questions concise."
        }
    ]

def generate_question(user_answer):
    global conversation_history

    conversation_history.append({
        "role": "user",
        "content": user_answer
    })

    response = requests.post(
        "http://localhost:11434/api/chat",
        json={
            "model": "llama3",
            "messages": conversation_history,
            "stream": False
        }
    )

    if response.status_code == 200:
        ai_response = response.json()["message"]["content"]
        
        conversation_history.append({
            "role": "assistant",
            "content": ai_response
        })
        
        return ai_response
    else:
        return "Sorry, I encountered an error connecting to the AI model."