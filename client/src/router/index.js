import { createRouter, createWebHistory } from 'vue-router'
import UserView from '../views/UserView.vue'
import Home from '../views/HomeView.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home,
  },
  {
    path: '/user',
    name: 'UserView',
    component: UserView,

    // pass the query string as a prop so the component can read it easily
    props: route => {
      const userName = route.query.userName || '';
      let userId;
      if (route.query.userId !== undefined) {
        const parsedId = Number(route.query.userId);
        userId = isNaN(parsedId) ? undefined : parsedId;
      }
      return { userName, userId };
    },
  }
]

export default createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})