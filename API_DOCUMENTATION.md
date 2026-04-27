## Help Post System

### Overview
The post system allows users to create, view, edit, and delete help posts in various categories. Users can request help or offer help, and then connect with other users via direct messaging.

---

## API Endpoints

### 1. **Get All Categories**
```
GET /api/help/help-categories
Authentication: None (public)

Response (200):
{
  "data": [
    { "category": "home" },
    { "category": "learning" },
    { "category": "garden" },
    { "category": "pet" },
    { "category": "creative" },
    { "category": "fitness" },
    { "category": "other" }
  ]
}
```

---

### 2. **Create a Post**
```
POST /api/help/help-posts
Authentication: Required (Bearer Token)

Request Body:
{
  "category": "home",          // Required: one of the allowed categories
  "postType": "request",        // Required: "request" or "offer"
  "title": "Need help with painting",
  "description": "Looking for someone to help paint my living room",
  "latitude": 40.7128,          // Optional: for location-based discovery
  "longitude": -74.0060        // Optional
}

Response (200):
{
  "id": 1,
  "createdByUserId": 5,
  "creatorUsername": "john_doe",
  "creatorName": "John Doe",
  "creatorDescription": "Experienced painter",
  "creatorProfilePictureUrl": "https://...",
  "creatorCredits": 25,
  "category": "home",
  "postType": "request",
  "title": "Need help with painting",
  "description": "Looking for someone to help paint my living room",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "distanceKm": null,
  "isOpen": true,
  "createdAtUtc": "2026-04-27T12:30:00Z"
}

Errors:
- 400: Missing required fields
- 400: Invalid category or post type
- 401: Not authenticated
```

---

### 3. **Get Posts (with Filtering)**
```
GET /api/help/help-posts
Authentication: Required (Bearer Token)

Query Parameters:
  category=home           // Optional: filter by category
  postType=request        // Optional: "request" or "offer"
  latitude=40.7128        // Optional: user's latitude
  longitude=-74.0060      // Optional: user's longitude
  radiusKm=10            // Optional: search radius (requires lat/lon)

Example:
GET /api/help/help-posts?category=home&postType=request&latitude=40.7128&longitude=-74.0060&radiusKm=10

Response (200):
{
  "data": [
    {
      "id": 1,
      "createdByUserId": 5,
      "creatorUsername": "john_doe",
      "creatorName": "John Doe",
      "creatorDescription": "Experienced painter",
      "creatorProfilePictureUrl": "https://...",
      "creatorCredits": 25,
      "category": "home",
      "postType": "request",
      "title": "Need help with painting",
      "description": "Looking for someone to help paint my living room",
      "latitude": 40.7128,
      "longitude": -74.0060,
      "distanceKm": 2.3,        // Calculated from user location
      "isOpen": true,
      "createdAtUtc": "2026-04-27T12:30:00Z"
    }
  ]
}

Notes:
- Posts are sorted by distance (if location provided) then by creation date (newest first)
- Only returns OPEN posts
- Distance is calculated automatically if latitude/longitude provided
```

---

### 4. **Get Single Post**
```
GET /api/help/help-posts/{postId}
Authentication: Required (Bearer Token)

Example:
GET /api/help/help-posts/1

Response (200):
{
  "id": 1,
  "createdByUserId": 5,
  "creatorUsername": "john_doe",
  "creatorName": "John Doe",
  "creatorDescription": "Experienced painter",
  "creatorProfilePictureUrl": "https://...",
  "creatorCredits": 25,
  "category": "home",
  "postType": "request",
  "title": "Need help with painting",
  "description": "Looking for someone to help paint my living room",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "distanceKm": null,
  "isOpen": true,
  "createdAtUtc": "2026-04-27T12:30:00Z"
}

Errors:
- 404: Post not found
- 401: Not authenticated
```

---

### 5. **Get My Posts**
```
GET /api/help/my-help-posts
Authentication: Required (Bearer Token)

Response (200):
{
  "data": [
    {
      "id": 1,
      "createdByUserId": 5,
      "creatorUsername": "john_doe",
      "creatorName": "John Doe",
      "creatorDescription": "Experienced painter",
      "creatorProfilePictureUrl": "https://...",
      "creatorCredits": 25,
      "category": "home",
      "postType": "request",
      "title": "Need help with painting",
      "description": "Looking for someone to help paint my living room",
      "latitude": 40.7128,
      "longitude": -74.0060,
      "distanceKm": null,
      "isOpen": true,
      "createdAtUtc": "2026-04-27T12:30:00Z"
    }
  ]
}

Notes:
- Returns all posts created by the current user
- Sorted by creation date (newest first)
```

---

### 6. **Update a Post**
```
PUT /api/help/help-posts/{postId}
Authentication: Required (Bearer Token)

Request Body (all optional - only include fields to update):
{
  "title": "Updated title",
  "description": "Updated description",
  "latitude": 40.7128,
  "longitude": -74.0060
}

Response (200):
{
  "id": 1,
  "createdByUserId": 5,
  "creatorUsername": "john_doe",
  "creatorName": "John Doe",
  "creatorDescription": "Experienced painter",
  "creatorProfilePictureUrl": "https://...",
  "creatorCredits": 25,
  "category": "home",
  "postType": "request",
  "title": "Updated title",
  "description": "Updated description",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "distanceKm": null,
  "isOpen": true,
  "createdAtUtc": "2026-04-27T12:30:00Z"
}

Errors:
- 404: Post not found or user doesn't own the post
- 401: Not authenticated
```

---

### 7. **Delete a Post**
```
DELETE /api/help/help-posts/{postId}
Authentication: Required (Bearer Token)

Response (200):
{
  "success": true,
  "message": "Help post deleted."
}

Errors:
- 404: Post not found or user doesn't own the post
- 401: Not authenticated
```

---

### 8. **Close a Post**
```
POST /api/help/help-posts/{postId}/close
Authentication: Required (Bearer Token)

Response (200):
{
  "success": true
}

Notes:
- Closes the post (isOpen = false)
- Different from delete - the post remains in the database
- Only the creator can close their own post

Errors:
- 404: Post not found or user doesn't own the post
- 401: Not authenticated
```

---

## Messaging Integration

### 9. **Start Chat / Message Creator**
```
POST /api/help/chats/start
Authentication: Required (Bearer Token)

Request Body:
{
  "helpPostId": 1,
  "message": "Hi, I'm interested in your post!"  // Optional initial message
}

Response (200):
{
  "id": 42,
  "helpPostId": 1,
  "initiatorUserId": 10,
  "recipientUserId": 5,
  "initiatorName": "Jane Smith",
  "recipientName": "John Doe",
  "status": "Active",
  "startedAtUtc": "2026-04-27T13:00:00Z",
  "endedAtUtc": null
}

Notes:
- Initiates a chat thread for a specific post
- If a chat already exists between the two users for this post, returns existing thread
- Automatically sends the initial message if provided
- Can't message your own posts

Errors:
- 404: Post not found or post is closed
- 400: Can't message your own post
- 401: Not authenticated
```

---

### 10. **Get My Chats**
```
GET /api/help/chats
Authentication: Required (Bearer Token)

Response (200):
{
  "data": [
    {
      "id": 42,
      "helpPostId": 1,
      "initiatorUserId": 10,
      "recipientUserId": 5,
      "initiatorName": "Jane Smith",
      "recipientName": "John Doe",
      "status": "Active",
      "startedAtUtc": "2026-04-27T13:00:00Z",
      "endedAtUtc": null
    }
  ]
}
```

---

### 11. **Send Message in Chat**
```
POST /api/help/chats/{chatId}/messages
Authentication: Required (Bearer Token)

Request Body:
{
  "message": "I can help with the painting!"
}

Response (200):
{
  "id": 123,
  "chatThreadId": 42,
  "senderUserId": 5,
  "message": "I can help with the painting!",
  "sentAtUtc": "2026-04-27T13:05:00Z"
}

Errors:
- 400: Missing message
- 404: Chat not found or no access
- 401: Not authenticated
```

---

### 12. **Get Chat Messages**
```
GET /api/help/chats/{chatId}/messages
Authentication: Required (Bearer Token)

Response (200):
{
  "data": [
    {
      "id": 122,
      "chatThreadId": 42,
      "senderUserId": 10,
      "message": "Hi, I'm interested in your post!",
      "sentAtUtc": "2026-04-27T13:00:00Z"
    },
    {
      "id": 123,
      "chatThreadId": 42,
      "senderUserId": 5,
      "message": "I can help with the painting!",
      "sentAtUtc": "2026-04-27T13:05:00Z"
    }
  ]
}

Errors:
- 404: Chat not found or no access
- 401: Not authenticated
```

---

### 13. **End Chat**
```
POST /api/help/chats/{chatId}/end
Authentication: Required (Bearer Token)

Response (200):
{
  "success": true,
  "message": "Chat ended."
}

Errors:
- 404: Chat not found or no access
- 401: Not authenticated
```

---

## Frontend Data Flow

### Creating & Viewing Posts
1. **Load Categories** → `GET /api/help/help-categories`
2. **User Creates Post** → `POST /api/help/help-posts` with category, type, title, description
3. **Display Posts** → `GET /api/help/help-posts?category=home&postType=request` (optional filters)
4. **View Single Post** → `GET /api/help/help-posts/{postId}` (for detail view)

### Editing & Deleting Posts
- **Edit Post** → `PUT /api/help/help-posts/{postId}` (user's own posts only)
- **Delete Post** → `DELETE /api/help/help-posts/{postId}` (user's own posts only)
- **Close Post** → `POST /api/help/help-posts/{postId}/close` (marks as inactive)

### Messaging Flow
1. **User Sees Post** → `GET /api/help/help-posts`
2. **Click "Message" Button** → `POST /api/help/chats/start` with `helpPostId`
3. **Redirect to Messaging Page** → Show chat thread with messages
4. **Load Previous Messages** → `GET /api/help/chats/{chatId}/messages`
5. **Send Message** → `POST /api/help/chats/{chatId}/messages`
6. **End Conversation** → `POST /api/help/chats/{chatId}/end`

---

## Creator User Data
Each post includes full creator profile information:
- `creatorUsername`: The user's unique identifier
- `creatorName`: Display name (optional, falls back to username)
- `creatorDescription`: Bio/description (optional)
- `creatorProfilePictureUrl`: Profile image URL
- `creatorCredits`: User's credit balance (for transactions)

This allows the frontend to display creator cards without additional API calls.

---

## Location-Based Features
- Posts store latitude/longitude coordinates
- When fetching posts with user location, distance is auto-calculated in kilometers
- Use `radiusKm` to filter results by distance
- Results are sorted by distance (nearest first) when location provided

---

## Error Response Format
```
{
  "message": "Error description"
}
```

Standard HTTP Status Codes:
- `200`: Success
- `400`: Bad request (validation error)
- `401`: Unauthorized (missing/invalid token)
- `404`: Resource not found
- `500`: Server error
