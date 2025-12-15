<template>
  <section class="home">

    <!-- Users List -->
    <div class="users-list" v-if="users && users.length">
      <div class="users-header">
        <h3>Users ({{ users.length }})</h3>
        <button @click="showUserForm = !showUserForm" class="toggle-user-btn">
          {{ showUserForm ? 'Cancel' : 'Create User' }}
        </button>
      </div>

      <CreateUserPopup
        v-if="showUserForm"
        :onClose="() => (showUserForm = false)"
        :onCreate="handleCreateUser"
      />
      <ul>
        <li
          v-for="u in users"
          :key="u.userId || u.UserId"
          class="user-item"
          @click="goToUserView(u)"
        >
          {{ u.userName || u.UserName }}
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import CreateUserPopup from './CreateUserPopup.vue' // Import the CreateUserPopup component
import createUser  from '@/assets/createUser.js'
import getUsers from '@/assets/getUsers.js'
// ----- Options for the dropdown ---------------------------------------
const options = [
  { value: 'NA', label: 'NA' },
  { value: 'EUW', label: 'EUW' },
  { value: 'EUNE', label: 'EUNE' },
  { value: 'KR', label: 'KR' },
  { value: 'JP', label: 'JP' },
  { value: 'LAN', label: 'LAN' },
  { value: 'LAS', label: 'LAS' },
  { value: 'OCE', label: 'OCE' },
  { value: 'RU', label: 'RU' },
  { value: 'TR', label: 'TR' },
]

// ----- Reactive state -------------------------------------------------
const query = ref('')
const tagLine = ref('EUNE')
const router = useRouter()

// User creation state
const showUserForm = ref(false)
const users = ref([])

// ----- Methods --------------------------------------------------------
function goToUserView(user) {
  const id = user?.userId ?? user?.UserId;
  const name = (user?.userName ?? user?.UserName ?? '').trim();
  if (!id || !name) return;

  const queryParams = { userId: id, userName: name };
  router.push({ name: 'UserView', query: queryParams });
}

async function handleCreateUser(userNames) {
  // Handle user creation logic here
  for (const userName of userNames) {
    const trimmedGameName = userName.gameName.trim();
    const trimmedTagline = userName.tagline.trim();
    
    if (trimmedGameName && trimmedTagline) {
      try {
        await createUser(trimmedGameName, trimmedTagline); // Call your API to create the user
        console.log(`User "${trimmedGameName}" created successfully!`);
        // Refresh the list after successful creation
        await loadUsers();
      } catch (err) {
        console.error(`Failed to create user "${trimmedGameName}":`, err.message);
      }
    }
  }
}

async function loadUsers() {
  try {
    users.value = await getUsers();
    console.log(`Loaded ${users.value.length} users`);
  } catch (e) {
    console.error('Failed to fetch users:', e?.message || e);
  }
}

onMounted(() => {
  loadUsers();
})
</script>

<style scoped>

.home {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-height: 100vh;
  padding: 2rem 1rem; /* Provide breathing room from the top */
  background-color: var(--color-bg);
  color: var(--color-text);
}

.home-form {  
  display: flex;
  gap: 0.1rem;
  margin-top: 1rem;
  align-items: center;
  justify-content: center;
}

.tagLine-select {
  padding: 0.5rem;
  font-size: 1rem;
  width: 90px;
}

.home-input {
  flex: 1;
  min-width: 0;
  width: 300px;
  padding: 0.5rem;
  font-size: 1rem;
}

.user-creation {
  margin-top: 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.toggle-user-btn,
.create-user-btn {
  padding: 0.5rem 1rem;
  font-size: 1rem;
  cursor: pointer;
  background-color: var(--color-primary);
  color: var(--color-text);
  border: none;
  border-radius: 6px;
}

.toggle-user-btn:hover,
.create-user-btn:hover {
  background-color: var(--color-primary-hover);
}

.user-form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  align-items: center;
}

.error {
  color: #dc3545;
  font-size: 0.9rem;
}

.success {
  color: #28a745;
  font-size: 0.9rem;
}

.users-list {
  margin: 2rem auto 0; /* center horizontally and add top spacing */
  width: 100%;
  max-width: 600px;
}

.users-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.users-list ul {
  list-style: none;
  margin: 0;
  padding: 0;
}

.user-item {
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  margin-bottom: 0.5rem;
  cursor: pointer;
  transition: background-color 0.15s ease, transform 0.05s ease;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
}

.user-item:hover {
  background-color: var(--color-bg-hover);
}

.user-item:active {
  transform: scale(0.99);
}
</style>