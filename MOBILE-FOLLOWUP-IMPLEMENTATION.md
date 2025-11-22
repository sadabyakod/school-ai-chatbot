# Mobile App Follow-Up Questions Implementation Guide

## 📱 Complete Implementation Guide for Flutter/Dart

### Overview
This guide provides step-by-step instructions to implement the intelligent follow-up questions feature in your Flutter mobile app. The backend automatically generates engaging follow-up questions and handles both positive and negative user responses.

---

## 🎯 Feature Behavior

### How It Works:
1. **After each AI response**, the API returns a `followUpQuestion` field
2. **Display as clickable chip** below the bot's message
3. **User can click** to automatically send that question
4. **User can respond freely**:
   - **Positive** ("yes", "sure", "ok") → AI continues with deeper explanation
   - **Negative** ("no", "nope", "different topic") → AI suggests 3 alternative topics
5. **Conversation flows naturally** with proper context awareness

---

## 📦 Step 1: Update Models

### ChatMessage Model
```dart
// lib/models/chat_message.dart
class ChatMessage {
  final String id;
  final String text;
  final bool isUser;
  final DateTime timestamp;
  final String? followUpQuestion; // Add this field

  ChatMessage({
    required this.id,
    required this.text,
    required this.isUser,
    required this.timestamp,
    this.followUpQuestion,
  });

  factory ChatMessage.fromJson(Map<String, dynamic> json, bool isUser) {
    return ChatMessage(
      id: json['id']?.toString() ?? DateTime.now().millisecondsSinceEpoch.toString(),
      text: isUser ? json['question'] : json['reply'],
      isUser: isUser,
      timestamp: DateTime.parse(json['timestamp']),
      followUpQuestion: isUser ? null : json['followUpQuestion'],
    );
  }
}
```

### API Response Model
```dart
// lib/models/chat_response.dart
class ChatResponse {
  final String status;
  final String sessionId;
  final String question;
  final String reply;
  final String? followUpQuestion; // NEW: Follow-up question
  final DateTime timestamp;

  ChatResponse({
    required this.status,
    required this.sessionId,
    required this.question,
    required this.reply,
    this.followUpQuestion,
    required this.timestamp,
  });

  factory ChatResponse.fromJson(Map<String, dynamic> json) {
    return ChatResponse(
      status: json['status'],
      sessionId: json['sessionId'],
      question: json['question'],
      reply: json['reply'],
      followUpQuestion: json['followUpQuestion'], // Parse this
      timestamp: DateTime.parse(json['timestamp']),
    );
  }
}
```

---

## 🌐 Step 2: API Service

### Chat Service with Follow-Up Handling
```dart
// lib/services/chat_service.dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/chat_response.dart';

class ChatService {
  static const String baseUrl = 'https://app-wlanqwy7vuwmu.azurewebsites.net';
  // For local testing: 'http://localhost:8080'
  
  String? _currentSessionId;

  Future<ChatResponse> sendMessage(String question) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/api/chat'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'question': question,
          'sessionId': _currentSessionId,
        }),
      ).timeout(const Duration(seconds: 30));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final chatResponse = ChatResponse.fromJson(data);
        
        // Save session ID for conversation continuity
        _currentSessionId = chatResponse.sessionId;
        
        return chatResponse;
      } else {
        throw Exception('Failed to send message: ${response.statusCode}');
      }
    } catch (e) {
      throw Exception('Error sending message: $e');
    }
  }

  // Start a new conversation
  void startNewSession() {
    _currentSessionId = null;
  }

  // Get current session ID
  String? get sessionId => _currentSessionId;
}
```

---

## 🎨 Step 3: UI Components

### Message Bubble with Follow-Up Question
```dart
// lib/widgets/message_bubble.dart
import 'package:flutter/material.dart';
import '../models/chat_message.dart';

class MessageBubble extends StatelessWidget {
  final ChatMessage message;
  final Function(String) onFollowUpTap;

  const MessageBubble({
    Key? key,
    required this.message,
    required this.onFollowUpTap,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: message.isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Column(
        crossAxisAlignment: message.isUser 
            ? CrossAxisAlignment.end 
            : CrossAxisAlignment.start,
        children: [
          // Main message bubble
          Container(
            margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
            padding: const EdgeInsets.all(12),
            constraints: BoxConstraints(
              maxWidth: MediaQuery.of(context).size.width * 0.75,
            ),
            decoration: BoxDecoration(
              color: message.isUser 
                  ? Colors.blue[600] 
                  : Colors.grey[200],
              borderRadius: BorderRadius.circular(16),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.1),
                  blurRadius: 4,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Text(
              message.text,
              style: TextStyle(
                color: message.isUser ? Colors.white : Colors.black87,
                fontSize: 15,
              ),
            ),
          ),
          
          // Follow-up question chip (only for bot messages)
          if (!message.isUser && message.followUpQuestion != null && message.followUpQuestion!.isNotEmpty)
            _buildFollowUpChip(context),
          
          // Timestamp
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 2),
            child: Text(
              _formatTime(message.timestamp),
              style: TextStyle(
                fontSize: 11,
                color: Colors.grey[600],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFollowUpChip(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(left: 8, right: 8, top: 8),
      constraints: BoxConstraints(
        maxWidth: MediaQuery.of(context).size.width * 0.75,
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () {
            // Haptic feedback
            HapticFeedback.lightImpact();
            onFollowUpTap(message.followUpQuestion!);
          },
          borderRadius: BorderRadius.circular(12),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [
                  Colors.purple[100]!,
                  Colors.pink[100]!,
                ],
              ),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: Colors.purple[300]!, width: 1),
              boxShadow: [
                BoxShadow(
                  color: Colors.purple.withOpacity(0.2),
                  blurRadius: 6,
                  offset: const Offset(0, 2),
                ),
              ],
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text(
                  '💡',
                  style: TextStyle(fontSize: 18),
                ),
                const SizedBox(width: 8),
                Flexible(
                  child: Text(
                    message.followUpQuestion!,
                    style: TextStyle(
                      color: Colors.purple[900],
                      fontWeight: FontWeight.w500,
                      fontSize: 14,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String _formatTime(DateTime dateTime) {
    final hour = dateTime.hour.toString().padLeft(2, '0');
    final minute = dateTime.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }
}
```

---

## 🎭 Step 4: Chat Screen Implementation

### Complete Chat Screen with Follow-Up Support
```dart
// lib/screens/chat_screen.dart
import 'package:flutter/material.dart';
import '../models/chat_message.dart';
import '../services/chat_service.dart';
import '../widgets/message_bubble.dart';

class ChatScreen extends StatefulWidget {
  const ChatScreen({Key? key}) : super(key: key);

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _textController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  final ChatService _chatService = ChatService();
  final List<ChatMessage> _messages = [];
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _addWelcomeMessage();
  }

  void _addWelcomeMessage() {
    setState(() {
      _messages.add(ChatMessage(
        id: '0',
        text: 'Welcome! I\'m your AI study assistant. How can I help you today?',
        isUser: false,
        timestamp: DateTime.now(),
      ));
    });
  }

  void _handleFollowUpTap(String question) {
    // Automatically send the follow-up question
    _handleSubmitted(question);
  }

  void _handleSubmitted(String text) async {
    if (text.trim().isEmpty || _isLoading) return;

    final userMessage = ChatMessage(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      text: text,
      isUser: true,
      timestamp: DateTime.now(),
    );

    setState(() {
      _messages.add(userMessage);
      _isLoading = true;
    });

    _textController.clear();
    _scrollToBottom();

    try {
      final response = await _chatService.sendMessage(text);

      final botMessage = ChatMessage(
        id: (DateTime.now().millisecondsSinceEpoch + 1).toString(),
        text: response.reply,
        isUser: false,
        timestamp: response.timestamp,
        followUpQuestion: response.followUpQuestion, // Include follow-up
      );

      setState(() {
        _messages.add(botMessage);
        _isLoading = false;
      });

      _scrollToBottom();
    } catch (e) {
      final errorMessage = ChatMessage(
        id: (DateTime.now().millisecondsSinceEpoch + 1).toString(),
        text: '😔 Oops! I couldn\'t reach the server. Please check your connection.',
        isUser: false,
        timestamp: DateTime.now(),
      );

      setState(() {
        _messages.add(errorMessage);
        _isLoading = false;
      });

      _scrollToBottom();

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error: $e')),
      );
    }
  }

  void _scrollToBottom() {
    Future.delayed(const Duration(milliseconds: 300), () {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  void _startNewConversation() {
    setState(() {
      _messages.clear();
      _chatService.startNewSession();
      _addWelcomeMessage();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('AI Study Assistant'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _startNewConversation,
            tooltip: 'New Conversation',
          ),
        ],
      ),
      body: Column(
        children: [
          // Messages list
          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              padding: const EdgeInsets.all(8),
              itemCount: _messages.length,
              itemBuilder: (context, index) {
                return MessageBubble(
                  message: _messages[index],
                  onFollowUpTap: _handleFollowUpTap,
                );
              },
            ),
          ),

          // Loading indicator
          if (_isLoading)
            Padding(
              padding: const EdgeInsets.all(8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.grey[200],
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        _buildDot(0),
                        const SizedBox(width: 4),
                        _buildDot(150),
                        const SizedBox(width: 4),
                        _buildDot(300),
                      ],
                    ),
                  ),
                ],
              ),
            ),

          // Input field
          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.1),
                  blurRadius: 4,
                  offset: const Offset(0, -2),
                ),
              ],
            ),
            child: Row(
              children: [
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.all(8),
                    child: TextField(
                      controller: _textController,
                      decoration: InputDecoration(
                        hintText: 'Ask me anything...',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                          borderSide: BorderSide.none,
                        ),
                        filled: true,
                        fillColor: Colors.grey[100],
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 12,
                        ),
                      ),
                      maxLines: null,
                      textInputAction: TextInputAction.send,
                      onSubmitted: _handleSubmitted,
                      enabled: !_isLoading,
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: CircleAvatar(
                    backgroundColor: Colors.blue[600],
                    child: IconButton(
                      icon: const Icon(Icons.send, color: Colors.white),
                      onPressed: _isLoading
                          ? null
                          : () => _handleSubmitted(_textController.text),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDot(int delay) {
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0.0, end: 1.0),
      duration: const Duration(milliseconds: 600),
      builder: (context, value, child) {
        return Opacity(
          opacity: (value * 2).clamp(0.0, 1.0),
          child: Container(
            width: 8,
            height: 8,
            decoration: const BoxDecoration(
              color: Colors.grey,
              shape: BoxShape.circle,
            ),
          ),
        );
      },
    );
  }

  @override
  void dispose() {
    _textController.dispose();
    _scrollController.dispose();
    super.dispose();
  }
}
```

---

## 🎯 Step 5: Handle Positive/Negative Responses

### The backend automatically handles:

**Positive Responses** (continues topic):
- "yes", "sure", "ok", "okay"
- "tell me more", "explain more", "continue"

**Negative Responses** (suggests alternatives):
- "no", "nope", "nah"
- "not interested", "don't"
- "different", "something else", "change topic"

### Example Flow:
```
Bot: "Here's info about photosynthesis..."
     💡 Would you like to learn about chloroplasts?

User taps chip or types: "yes"
Bot: [Deeper explanation about chloroplasts...]
     💡 Should we explore how light is converted to energy?

User types: "no"
Bot: "No problem! Let me suggest some other topics:
      1. Plant Respiration - How plants breathe
      2. Cellular Structure - Building blocks
      3. Plant Hormones - Growth mechanisms
      
      Which one interests you?"
```

---

## 📦 Step 6: Dependencies

Add to `pubspec.yaml`:
```yaml
dependencies:
  flutter:
    sdk: flutter
  http: ^1.1.0
  intl: ^0.18.1  # For date formatting
```

---

## 🔧 Step 7: Testing

### Test Positive Response:
```dart
await chatService.sendMessage('What is photosynthesis?');
// Response includes followUpQuestion
await chatService.sendMessage('yes'); // or 'sure', 'ok'
// Gets deeper explanation of same topic
```

### Test Negative Response:
```dart
await chatService.sendMessage('What is photosynthesis?');
// Response includes followUpQuestion
await chatService.sendMessage('no'); // or 'nope', 'different'
// Gets 3 alternative topic suggestions
```

---

## 🎨 Optional: Enhanced Animations

### Add smooth animations with AnimatedOpacity:
```dart
AnimatedOpacity(
  opacity: message.followUpQuestion != null ? 1.0 : 0.0,
  duration: const Duration(milliseconds: 300),
  child: _buildFollowUpChip(context),
)
```

### Add scale animation on tap:
```dart
AnimatedScale(
  scale: _isPressed ? 0.95 : 1.0,
  duration: const Duration(milliseconds: 100),
  child: _buildFollowUpChip(context),
)
```

---

## 🚀 Production Tips

1. **Error Handling**: Always handle network errors gracefully
2. **Loading States**: Show clear indicators during API calls
3. **Offline Support**: Queue messages when offline, sync when online
4. **Session Management**: Save sessionId locally with SharedPreferences
5. **Haptic Feedback**: Add tactile response on follow-up chip taps
6. **Accessibility**: Ensure follow-up chips are accessible with screen readers
7. **Rate Limiting**: Implement debouncing for rapid-fire messages

---

## 📚 API Reference

### Endpoint
```
POST https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat
```

### Request
```json
{
  "question": "What is photosynthesis?",
  "sessionId": "optional-uuid"
}
```

### Response
```json
{
  "status": "success",
  "sessionId": "abc-123-def-456",
  "question": "What is photosynthesis?",
  "reply": "Photosynthesis is the process by which plants...",
  "followUpQuestion": "Would you like to learn about chloroplasts?",
  "timestamp": "2025-11-23T10:00:00.0000000Z"
}
```

---

## ✅ Checklist

- [ ] Updated ChatMessage model with `followUpQuestion` field
- [ ] Created ChatResponse model with follow-up parsing
- [ ] Implemented ChatService with session management
- [ ] Built MessageBubble with follow-up chip
- [ ] Added tap handler for follow-up questions
- [ ] Implemented ChatScreen with loading states
- [ ] Tested positive responses ("yes", "sure")
- [ ] Tested negative responses ("no", "different")
- [ ] Added error handling and retry logic
- [ ] Implemented smooth scrolling and animations
- [ ] Added haptic feedback
- [ ] Tested on both iOS and Android

---

## 🎉 Result

You now have a fully functional AI chat with intelligent follow-up questions that:
- ✅ Automatically generates engaging follow-up questions
- ✅ Displays them as beautiful clickable chips
- ✅ Handles positive responses by continuing the topic
- ✅ Handles negative responses by suggesting alternatives
- ✅ Maintains conversation context across messages
- ✅ Provides smooth UX with animations and feedback

Happy coding! 🚀
